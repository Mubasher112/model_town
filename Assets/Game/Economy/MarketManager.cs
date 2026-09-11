using System;
using System.Collections.Generic;
using Game.Data;
using Game.Economy;
using Game.Inventory;
using Game.Player;
using Game.Services;

namespace Game.Economy
{
    public enum MarketOperationResult
    {
        Success,
        InvalidItem,
        ItemNotBuyable,
        ItemNotSellable,
        LevelLocked,
        InsufficientCoins,
        InsufficientStorage,
        InsufficientInventory,
        InsufficientStock,
        InvalidQuantity
    }

    public class MarketManager
    {
        private readonly EconomyManager _economyManager;
        private readonly InventoryManager _inventoryManager;
        private readonly PlayerProfile _playerProfile;
        private readonly IGameTimeService _timeService;

        private readonly Dictionary<string, MarketListing> _listings = new Dictionary<string, MarketListing>();
        private readonly List<MarketTransaction> _history = new List<MarketTransaction>();

        public int MaxHistoryLimit { get; set; } = 50;

        public event Action OnMarketUpdated;
        public event Action<string> OnNotificationMessage;

        public MarketManager(
            EconomyManager economyManager,
            InventoryManager inventoryManager,
            PlayerProfile playerProfile,
            IGameTimeService timeService,
            List<MarketListing> savedListings = null,
            List<MarketTransaction> savedHistory = null)
        {
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));

            InitializeListings(savedListings);
            if (savedHistory != null)
            {
                _history.AddRange(savedHistory);
            }

            RecalculateRestocks();
        }

        private void InitializeListings(List<MarketListing> savedListings)
        {
            if (savedListings != null && savedListings.Count > 0)
            {
                foreach (var listing in savedListings)
                {
                    _listings[listing.ItemId] = listing;
                }
            }

            long now = _timeService.CurrentUtcTicks;
            foreach (var config in MarketLibrary.GetAllMarketItems())
            {
                if (!_listings.ContainsKey(config.ItemId))
                {
                    _listings[config.ItemId] = new MarketListing(config.ItemId, config.MaxStock, now);
                }
            }
        }

        public void RecalculateRestocks()
        {
            long now = _timeService.CurrentUtcTicks;
            bool updated = false;

            foreach (var config in MarketLibrary.GetAllMarketItems())
            {
                if (!_listings.TryGetValue(config.ItemId, out var listing)) continue;

                if (listing.CurrentStock >= config.MaxStock)
                {
                    listing.LastRestockUtcTicks = now;
                    continue;
                }

                long elapsedTicks = now - listing.LastRestockUtcTicks;
                if (elapsedTicks <= 0) continue;

                double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;
                if (config.RestockIntervalSeconds <= 0) continue;

                int restockCycles = (int)(elapsedSeconds / config.RestockIntervalSeconds);
                if (restockCycles > 0)
                {
                    int totalRestock = restockCycles * config.RestockAmount;
                    listing.CurrentStock = Math.Min(config.MaxStock, listing.CurrentStock + totalRestock);
                    long ticksConsumed = TimeSpan.FromSeconds(restockCycles * config.RestockIntervalSeconds).Ticks;
                    listing.LastRestockUtcTicks += ticksConsumed;
                    updated = true;
                }
            }

            if (updated)
            {
                OnMarketUpdated?.Invoke();
            }
        }

        public MarketListing GetListing(string itemId)
        {
            RecalculateRestocks();
            return _listings.TryGetValue(itemId, out var listing) ? listing : null;
        }

        public MarketOperationResult CanBuyItem(string itemId, int quantity, out long totalCost)
        {
            totalCost = 0;
            if (quantity <= 0) return MarketOperationResult.InvalidQuantity;

            var config = MarketLibrary.GetItemConfig(itemId);
            if (config == null || !config.IsBuyable) return MarketOperationResult.InvalidItem;

            if (_playerProfile.Level < config.UnlockLevel) return MarketOperationResult.LevelLocked;

            var listing = GetListing(itemId);
            if (listing == null || listing.CurrentStock < quantity) return MarketOperationResult.InsufficientStock;

            if (!_inventoryManager.CanAddItem(quantity)) return MarketOperationResult.InsufficientStorage;

            totalCost = config.BaseBuyPrice * quantity;
            if (!_economyManager.CanAffordCoins(totalCost)) return MarketOperationResult.InsufficientCoins;

            return MarketOperationResult.Success;
        }

        public MarketOperationResult BuyItem(string itemId, int quantity)
        {
            RecalculateRestocks();

            var checkResult = CanBuyItem(itemId, quantity, out long totalCost);
            if (checkResult != MarketOperationResult.Success)
            {
                return checkResult;
            }

            var config = MarketLibrary.GetItemConfig(itemId);
            var listing = GetListing(itemId);

            // ATOMIC EXECUTION
            if (!_economyManager.SpendCoins(totalCost))
            {
                return MarketOperationResult.InsufficientCoins;
            }

            listing.CurrentStock -= quantity;
            string itemName = FormatItemName(itemId, config.Name);
            _inventoryManager.AddItem(itemId, itemName, GetItemType(config.Category), quantity);

            RecordTransaction(TransactionType.Buy, itemId, itemName, quantity, config.BaseBuyPrice, totalCost);

            OnNotificationMessage?.Invoke($"Purchased {itemName} ×{quantity} for {totalCost:N0} Coins");
            OnMarketUpdated?.Invoke();

            return MarketOperationResult.Success;
        }

        public MarketOperationResult CanSellItem(string itemId, int quantity, out long totalEarnings)
        {
            totalEarnings = 0;
            if (quantity <= 0) return MarketOperationResult.InvalidQuantity;

            var config = MarketLibrary.GetItemConfig(itemId);
            if (config == null || !config.IsSellable) return MarketOperationResult.InvalidItem;

            if (_playerProfile.Level < config.UnlockLevel) return MarketOperationResult.LevelLocked;

            if (_inventoryManager.GetQuantity(itemId) < quantity) return MarketOperationResult.InsufficientInventory;

            totalEarnings = config.BaseSellPrice * quantity;
            return MarketOperationResult.Success;
        }

        public MarketOperationResult SellItem(string itemId, int quantity)
        {
            RecalculateRestocks();

            var checkResult = CanSellItem(itemId, quantity, out long totalEarnings);
            if (checkResult != MarketOperationResult.Success)
            {
                return checkResult;
            }

            var config = MarketLibrary.GetItemConfig(itemId);

            // ATOMIC EXECUTION
            if (!_inventoryManager.RemoveItem(itemId, quantity))
            {
                return MarketOperationResult.InsufficientInventory;
            }

            _economyManager.EarnCoins(totalEarnings);

            string itemName = FormatItemName(itemId, config.Name);
            RecordTransaction(TransactionType.Sell, itemId, itemName, quantity, config.BaseSellPrice, totalEarnings);

            OnNotificationMessage?.Invoke($"Sold {itemName} ×{quantity} for +{totalEarnings:N0} Coins");
            OnMarketUpdated?.Invoke();

            return MarketOperationResult.Success;
        }

        private void RecordTransaction(TransactionType type, string itemId, string itemName, int quantity, long unitPrice, long totalPrice)
        {
            var tx = new MarketTransaction(
                "tx_" + Guid.NewGuid().ToString().Substring(0, 8),
                type,
                itemId,
                itemName,
                quantity,
                unitPrice,
                totalPrice,
                _timeService.CurrentUtcTicks
            );

            _history.Insert(0, tx);
            if (_history.Count > MaxHistoryLimit)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        private string FormatItemName(string itemId, string configName)
        {
            return !string.IsNullOrEmpty(configName) ? configName : itemId;
        }

        private ItemType GetItemType(MarketCategory category)
        {
            switch (category)
            {
                case MarketCategory.Crops: return ItemType.Crop;
                case MarketCategory.RawMaterials: return ItemType.RawMaterial;
                case MarketCategory.ProcessedGoods: return ItemType.ManufacturedGood;
                default: return ItemType.RawMaterial;
            }
        }

        public List<MarketListing> ExportListings() => new List<MarketListing>(_listings.Values);
        public List<MarketTransaction> ExportHistory() => new List<MarketTransaction>(_history);

        public void DevRestockAll()
        {
            long now = _timeService.CurrentUtcTicks;
            foreach (var config in MarketLibrary.GetAllMarketItems())
            {
                _listings[config.ItemId] = new MarketListing(config.ItemId, config.MaxStock, now);
            }
            OnMarketUpdated?.Invoke();
        }

        public void DevEmptyStock()
        {
            foreach (var listing in _listings.Values)
            {
                listing.CurrentStock = 0;
            }
            OnMarketUpdated?.Invoke();
        }

        public void DevClearHistory()
        {
            _history.Clear();
            OnMarketUpdated?.Invoke();
        }
    }
}
