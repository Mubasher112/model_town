using System;
using System.Collections.Generic;
using Game.Inventory;

namespace Game.Data
{
    [Serializable]
    public class ItemConfig
    {
        public string ItemId;
        public string Name;
        public ItemType Type;
        public long BuyPrice;
        public long SellPrice;
        public int UnlockLevel;
    }

    [Serializable]
    public class BuildingConfig
    {
        public string BuildingId;
        public string Name;
        public int Width = 1;
        public int Height = 1;
        public long BuildCostCoins;
        public int BuildCostGems;
        public float BuildTimeSeconds;
        public int UnlockLevel;
        public int XpReward;
    }

    [Serializable]
    public class CropConfig
    {
        public string CropId;
        public string Name;
        public float GrowthTimeSeconds;
        public long SeedCostCoins;
        public int HarvestXp;
        public string HarvestItemId;
        public int HarvestItemYield = 1;
    }

    [Serializable]
    public class RecipeIngredient
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public class RecipeConfig
    {
        public string RecipeId;
        public string BuildingId;
        public string OutputItemId;
        public int OutputQuantity = 1;
        public float ProductionTimeSeconds;
        public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();
    }
}
