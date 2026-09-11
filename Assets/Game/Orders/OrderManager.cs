using System;
using System.Collections.Generic;
using Game.Data;
using Game.Economy;
using Game.Inventory;
using Game.Player;
using Game.Services;

namespace Game.Orders
{
    public enum OrderOperationResult
    {
        Success,
        InvalidOrder,
        OrderExpired,
        OrderAlreadyCompleted,
        MissingRequirements,
        OrderSlotFull
    }

    public class OrderManager
    {
        private readonly List<OrderInstance> _activeOrders = new List<OrderInstance>();
        private readonly List<OrderHistoryEntry> _history = new List<OrderHistoryEntry>();
        private readonly InventoryManager _inventoryManager;
        private readonly EconomyManager _economyManager;
        private readonly PlayerProfile _playerProfile;
        private readonly StandardGameTimeService _timeService;

        public int MaxActiveOrderSlots { get; set; } = 3;
        public int MaxHistoryEntries { get; set; } = 20;

        public event Action OnOrdersUpdated;
        public event Action<string> OnNotificationMessage;

        public OrderManager(
            InventoryManager inventoryManager,
            EconomyManager economyManager,
            PlayerProfile playerProfile,
            StandardGameTimeService timeService = null,
            int maxSlots = 3)
        {
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? new StandardGameTimeService();
            MaxActiveOrderSlots = maxSlots;
        }

        public List<OrderInstance> GetActiveOrders()
        {
            return new List<OrderInstance>(_activeOrders);
        }

        public List<OrderHistoryEntry> GetOrderHistory()
        {
            return new List<OrderHistoryEntry>(_history);
        }

        public void LoadActiveOrders(List<OrderInstance> orders)
        {
            _activeOrders.Clear();
            if (orders != null)
            {
                _activeOrders.AddRange(orders);
            }
            CheckAndExpireOrders();
            EnsureMinimumOrders();
        }

        public void LoadOrderHistory(List<OrderHistoryEntry> history)
        {
            _history.Clear();
            if (history != null)
            {
                _history.AddRange(history);
            }
        }

        public void CheckAndExpireOrders()
        {
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            bool updated = false;
            for (int i = _activeOrders.Count - 1; i >= 0; i--)
            {
                var order = _activeOrders[i];
                if (order.State == OrderState.Active && order.IsExpired(nowTicks))
                {
                    order.State = OrderState.Expired;
                    _activeOrders.RemoveAt(i);
                    updated = true;
                }
            }

            if (updated)
            {
                EnsureMinimumOrders();
                OnOrdersUpdated?.Invoke();
            }
        }

        public void EnsureMinimumOrders()
        {
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            while (_activeOrders.Count < MaxActiveOrderSlots)
            {
                var newOrder = OrderGenerator.GenerateOrderForLevel(_playerProfile.Level, nowTicks);
                _activeOrders.Add(newOrder);
            }
            OnOrdersUpdated?.Invoke();
        }

        public bool ValidateOrder(OrderInstance order)
        {
            if (order == null || order.State != OrderState.Active) return false;

            _timeService.UpdateCurrentTime();
            if (order.IsExpired(_timeService.CurrentUtcTicks)) return false;

            if (order.Requirements == null) return true;

            foreach (var req in order.Requirements)
            {
                if (_inventoryManager.GetQuantity(req.ItemId) < req.Quantity)
                {
                    return false;
                }
            }
            return true;
        }

        public OrderOperationResult FulfillOrder(string orderId)
        {
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            OrderInstance targetOrder = null;
            foreach (var o in _activeOrders)
            {
                if (o.OrderId == orderId)
                {
                    targetOrder = o;
                    break;
                }
            }

            if (targetOrder == null) return OrderOperationResult.InvalidOrder;
            if (targetOrder.State == OrderState.Completed) return OrderOperationResult.OrderAlreadyCompleted;
            if (targetOrder.IsExpired(nowTicks))
            {
                targetOrder.State = OrderState.Expired;
                _activeOrders.Remove(targetOrder);
                EnsureMinimumOrders();
                OnOrdersUpdated?.Invoke();
                return OrderOperationResult.OrderExpired;
            }

            // ATOMIC VALIDATION: Check all requirements prior to any deduction
            if (!ValidateOrder(targetOrder))
            {
                OnNotificationMessage?.Invoke("Not enough products");
                return OrderOperationResult.MissingRequirements;
            }

            // ATOMIC DEDUCTION: Remove all items
            foreach (var req in targetOrder.Requirements)
            {
                _inventoryManager.RemoveItem(req.ItemId, req.Quantity);
            }

            // ATOMIC GRANT: Award Coins and XP
            _economyManager.EarnCoins(targetOrder.Reward.Coins);
            _playerProfile.AddXP(targetOrder.Reward.Xp);

            // Mark complete and record history
            targetOrder.State = OrderState.Completed;
            _activeOrders.Remove(targetOrder);

            AddHistoryEntry(new OrderHistoryEntry(
                targetOrder.OrderId,
                targetOrder.CustomerId,
                targetOrder.Reward.Coins,
                targetOrder.Reward.Xp,
                nowTicks
            ));

            // Generate replacement order
            EnsureMinimumOrders();

            OnNotificationMessage?.Invoke($"Order Complete! +{targetOrder.Reward.Coins} Coins, +{targetOrder.Reward.Xp} XP");
            OnOrdersUpdated?.Invoke();

            return OrderOperationResult.Success;
        }

        private void AddHistoryEntry(OrderHistoryEntry entry)
        {
            _history.Insert(0, entry);
            if (_history.Count > MaxHistoryEntries)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        public void DevGenerateNewOrder()
        {
            _timeService.UpdateCurrentTime();
            var newOrder = OrderGenerator.GenerateOrderForLevel(_playerProfile.Level, _timeService.CurrentUtcTicks);
            if (_activeOrders.Count >= MaxActiveOrderSlots)
            {
                _activeOrders.RemoveAt(0);
            }
            _activeOrders.Add(newOrder);
            OnOrdersUpdated?.Invoke();
        }

        public void DevClearHistory()
        {
            _history.Clear();
            OnOrdersUpdated?.Invoke();
        }
    }
}
