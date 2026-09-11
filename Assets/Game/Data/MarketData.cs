using System;
using System.Collections.Generic;

namespace Game.Data
{
    public enum MarketCategory
    {
        Crops,
        RawMaterials,
        ProcessedGoods
    }

    public enum TransactionType
    {
        Buy,
        Sell
    }

    [Serializable]
    public class MarketItemConfig
    {
        public string ItemId;
        public string Name;
        public MarketCategory Category;
        public bool IsBuyable = true;
        public bool IsSellable = true;
        public long BaseBuyPrice = 10;
        public long BaseSellPrice = 6;
        public int UnlockLevel = 1;
        public int MaxStock = 100;
        public int RestockAmount = 20;
        public float RestockIntervalSeconds = 1800f; // 30 minutes

        public MarketItemConfig() { }

        public MarketItemConfig(
            string itemId,
            string name,
            MarketCategory category,
            long baseBuyPrice,
            long baseSellPrice,
            int unlockLevel = 1,
            int maxStock = 100,
            int restockAmount = 20,
            float restockIntervalSeconds = 1800f)
        {
            ItemId = itemId;
            Name = name;
            Category = category;
            BaseBuyPrice = baseBuyPrice;
            BaseSellPrice = baseSellPrice;
            UnlockLevel = unlockLevel;
            MaxStock = maxStock;
            RestockAmount = restockAmount;
            RestockIntervalSeconds = restockIntervalSeconds;
            IsBuyable = true;
            IsSellable = true;
        }
    }

    public static class MarketLibrary
    {
        private static readonly Dictionary<string, MarketItemConfig> _items = new Dictionary<string, MarketItemConfig>
        {
            // Crops
            { "crop_wheat", new MarketItemConfig("crop_wheat", "Wheat", MarketCategory.Crops, baseBuyPrice: 10, baseSellPrice: 6, unlockLevel: 1, maxStock: 100, restockAmount: 20, restockIntervalSeconds: 1800f) },
            { "crop_corn", new MarketItemConfig("crop_corn", "Corn", MarketCategory.Crops, baseBuyPrice: 20, baseSellPrice: 12, unlockLevel: 2, maxStock: 80, restockAmount: 15, restockIntervalSeconds: 1800f) },
            { "crop_carrot", new MarketItemConfig("crop_carrot", "Carrot", MarketCategory.Crops, baseBuyPrice: 35, baseSellPrice: 20, unlockLevel: 3, maxStock: 60, restockAmount: 10, restockIntervalSeconds: 2400f) },
            { "crop_sugarcane", new MarketItemConfig("crop_sugarcane", "Sugarcane", MarketCategory.Crops, baseBuyPrice: 50, baseSellPrice: 30, unlockLevel: 5, maxStock: 50, restockAmount: 10, restockIntervalSeconds: 3000f) },
            { "crop_tomato", new MarketItemConfig("crop_tomato", "Tomato", MarketCategory.Crops, baseBuyPrice: 70, baseSellPrice: 42, unlockLevel: 7, maxStock: 40, restockAmount: 8, restockIntervalSeconds: 3600f) },

            // Raw Materials
            { "item_wood", new MarketItemConfig("item_wood", "Wood", MarketCategory.RawMaterials, baseBuyPrice: 15, baseSellPrice: 9, unlockLevel: 1, maxStock: 80, restockAmount: 20, restockIntervalSeconds: 1800f) },
            { "item_stone", new MarketItemConfig("item_stone", "Stone", MarketCategory.RawMaterials, baseBuyPrice: 20, baseSellPrice: 12, unlockLevel: 1, maxStock: 80, restockAmount: 20, restockIntervalSeconds: 1800f) },
            { "item_clay", new MarketItemConfig("item_clay", "Clay", MarketCategory.RawMaterials, baseBuyPrice: 30, baseSellPrice: 18, unlockLevel: 2, maxStock: 60, restockAmount: 15, restockIntervalSeconds: 2400f) },
            { "item_ore", new MarketItemConfig("item_ore", "Iron Ore", MarketCategory.RawMaterials, baseBuyPrice: 45, baseSellPrice: 27, unlockLevel: 3, maxStock: 50, restockAmount: 10, restockIntervalSeconds: 3000f) },
            { "item_milk", new MarketItemConfig("item_milk", "Milk", MarketCategory.RawMaterials, baseBuyPrice: 40, baseSellPrice: 24, unlockLevel: 4, maxStock: 50, restockAmount: 10, restockIntervalSeconds: 2400f) },

            // Processed Goods
            { "item_animal_feed", new MarketItemConfig("item_animal_feed", "Animal Feed", MarketCategory.ProcessedGoods, baseBuyPrice: 25, baseSellPrice: 15, unlockLevel: 1, maxStock: 60, restockAmount: 15, restockIntervalSeconds: 1800f) },
            { "item_flour", new MarketItemConfig("item_flour", "Flour", MarketCategory.ProcessedGoods, baseBuyPrice: 30, baseSellPrice: 18, unlockLevel: 2, maxStock: 50, restockAmount: 12, restockIntervalSeconds: 2400f) },
            { "item_bread", new MarketItemConfig("item_bread", "Bread", MarketCategory.ProcessedGoods, baseBuyPrice: 60, baseSellPrice: 36, unlockLevel: 2, maxStock: 40, restockAmount: 8, restockIntervalSeconds: 3000f) },
            { "item_sugar", new MarketItemConfig("item_sugar", "Sugar", MarketCategory.ProcessedGoods, baseBuyPrice: 55, baseSellPrice: 33, unlockLevel: 3, maxStock: 40, restockAmount: 8, restockIntervalSeconds: 3000f) },
            { "item_brick", new MarketItemConfig("item_brick", "Brick", MarketCategory.ProcessedGoods, baseBuyPrice: 75, baseSellPrice: 45, unlockLevel: 3, maxStock: 30, restockAmount: 5, restockIntervalSeconds: 3600f) }
        };

        public static MarketItemConfig GetItemConfig(string itemId)
        {
            return _items.TryGetValue(itemId, out var config) ? config : null;
        }

        public static List<MarketItemConfig> GetAllMarketItems()
        {
            return new List<MarketItemConfig>(_items.Values);
        }

        public static List<MarketItemConfig> GetMarketItemsByCategory(MarketCategory category)
        {
            var list = new List<MarketItemConfig>();
            foreach (var item in _items.Values)
            {
                if (item.Category == category) list.Add(item);
            }
            return list;
        }
    }

    [Serializable]
    public class MarketListing
    {
        public string ItemId;
        public int CurrentStock;
        public long LastRestockUtcTicks;

        public MarketListing() { }

        public MarketListing(string itemId, int initialStock, long lastRestockUtcTicks)
        {
            ItemId = itemId;
            CurrentStock = initialStock;
            LastRestockUtcTicks = lastRestockUtcTicks;
        }
    }

    [Serializable]
    public class MarketTransaction
    {
        public string TransactionId;
        public TransactionType Type;
        public string ItemId;
        public string ItemName;
        public int Quantity;
        public long UnitPrice;
        public long TotalPrice;
        public long UtcTicks;

        public MarketTransaction() { }

        public MarketTransaction(
            string transactionId,
            TransactionType type,
            string itemId,
            string itemName,
            int quantity,
            long unitPrice,
            long totalPrice,
            long utcTicks)
        {
            TransactionId = transactionId;
            Type = type;
            ItemId = itemId;
            ItemName = itemName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            TotalPrice = totalPrice;
            UtcTicks = utcTicks;
        }
    }
}
