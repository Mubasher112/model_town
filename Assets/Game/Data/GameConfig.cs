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
        public string SeedItemId;
        public string HarvestItemId;
        public float GrowthTimeSeconds;
        public int HarvestQuantity = 2;
        public int HarvestXp = 5;
        public long SeedCostCoins = 10;
        public int UnlockLevel = 1;

        public CropConfig() { }

        public CropConfig(
            string cropId,
            string name,
            string seedItemId,
            string harvestItemId,
            float growthTimeSeconds,
            int harvestQuantity = 2,
            int harvestXp = 5,
            long seedCostCoins = 10,
            int unlockLevel = 1)
        {
            CropId = cropId;
            Name = name;
            SeedItemId = seedItemId;
            HarvestItemId = harvestItemId;
            GrowthTimeSeconds = growthTimeSeconds;
            HarvestQuantity = harvestQuantity;
            HarvestXp = harvestXp;
            SeedCostCoins = seedCostCoins;
            UnlockLevel = unlockLevel;
        }
    }

    public static class CropLibrary
    {
        private static readonly Dictionary<string, CropConfig> _crops = new Dictionary<string, CropConfig>
        {
            {
                "wheat",
                new CropConfig("wheat", "Wheat", "seed_wheat", "crop_wheat", 10f, harvestQuantity: 2, harvestXp: 5, seedCostCoins: 5, unlockLevel: 1)
            },
            {
                "corn",
                new CropConfig("corn", "Corn", "seed_corn", "crop_corn", 30f, harvestQuantity: 2, harvestXp: 12, seedCostCoins: 15, unlockLevel: 2)
            },
            {
                "carrot",
                new CropConfig("carrot", "Carrot", "seed_carrot", "crop_carrot", 60f, harvestQuantity: 2, harvestXp: 20, seedCostCoins: 25, unlockLevel: 3)
            },
            {
                "sugarcane",
                new CropConfig("sugarcane", "Sugarcane", "seed_sugarcane", "crop_sugarcane", 120f, harvestQuantity: 3, harvestXp: 35, seedCostCoins: 40, unlockLevel: 5)
            },
            {
                "tomato",
                new CropConfig("tomato", "Tomato", "seed_tomato", "crop_tomato", 180f, harvestQuantity: 3, harvestXp: 50, seedCostCoins: 60, unlockLevel: 7)
            }
        };

        public static CropConfig GetCrop(string cropId)
        {
            return _crops.TryGetValue(cropId, out var config) ? config : null;
        }

        public static List<CropConfig> GetAllCrops()
        {
            return new List<CropConfig>(_crops.Values);
        }
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
