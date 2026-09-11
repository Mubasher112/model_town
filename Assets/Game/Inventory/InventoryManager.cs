using System;
using System.Collections.Generic;

namespace Game.Inventory
{
    public enum ItemType
    {
        Crop,
        Seed,
        RawMaterial,
        ManufacturedGood,
        ConstructionSupply,
        Food,
        Decoration
    }

    [Serializable]
    public class InventoryItem
    {
        public string ItemId;
        public string ItemName;
        public ItemType Type;
        public int Quantity;

        public InventoryItem() { }

        public InventoryItem(string itemId, string itemName, ItemType type, int quantity)
        {
            ItemId = itemId;
            ItemName = itemName;
            Type = type;
            Quantity = quantity;
        }
    }

    [Serializable]
    public class InventoryManager
    {
        private readonly Dictionary<string, InventoryItem> _items = new Dictionary<string, InventoryItem>();
        public int MaxCapacity = 100;

        [NonSerialized]
        public Action OnInventoryChanged;

        public int CurrentCount { get; private set; }

        public InventoryManager(int maxCapacity = 100)
        {
            MaxCapacity = maxCapacity;
            CurrentCount = 0;
        }

        public bool CanAddItem(int amount)
        {
            if (amount <= 0) return false;
            return CurrentCount + amount <= MaxCapacity;
        }

        public bool AddItem(string itemId, string itemName, ItemType type, int amount)
        {
            if (amount <= 0 || !CanAddItem(amount)) return false;

            if (_items.TryGetValue(itemId, out var item))
            {
                item.Quantity += amount;
            }
            else
            {
                _items[itemId] = new InventoryItem(itemId, itemName, type, amount);
            }

            CurrentCount += amount;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            if (amount <= 0) return false;
            if (!_items.TryGetValue(itemId, out var item) || item.Quantity < amount) return false;

            item.Quantity -= amount;
            CurrentCount -= amount;

            if (item.Quantity == 0)
            {
                _items.Remove(itemId);
            }

            OnInventoryChanged?.Invoke();
            return true;
        }

        public int GetQuantity(string itemId)
        {
            return _items.TryGetValue(itemId, out var item) ? item.Quantity : 0;
        }

        public List<InventoryItem> GetAllItems()
        {
            return new List<InventoryItem>(_items.Values);
        }

        public void Clear()
        {
            _items.Clear();
            CurrentCount = 0;
            OnInventoryChanged?.Invoke();
        }
    }
}
