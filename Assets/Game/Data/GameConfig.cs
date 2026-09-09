using System;
using System.Collections.Generic;
using Game.Inventory;

namespace Game.Data
{
    public enum BuildingCategory
    {
        Residential,
        Farming,
        Storage,
        Community,
        Decoration,
        Production
    }

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
        public string Description;
        public BuildingCategory Category;
        public int Width = 1;
        public int Height = 1;
        public long BuildCostCoins;
        public int BuildCostGems;
        public List<RecipeIngredient> RequiredMaterials = new List<RecipeIngredient>();
        public float ConstructionTimeSeconds;
        public int UnlockLevel = 1;
        public int XpReward = 10;
        public int PopulationCapacity = 0;
        public int StorageCapacityBonus = 0;
        public int MaxUpgradeLevel = 2;

        // Upgrade config
        public long UpgradeCostCoins = 200;
        public float UpgradeTimeSeconds = 30f;
        public int UpgradePopulationBonus = 5;
        public int UpgradeStorageBonus = 20;

        public BuildingConfig() { }

        public BuildingConfig(
            string buildingId,
            string name,
            string description,
            BuildingCategory category,
            int width,
            int height,
            long buildCostCoins,
            float constructionTimeSeconds,
            int unlockLevel = 1,
            int xpReward = 10,
            int populationCapacity = 0,
            int storageCapacityBonus = 0)
        {
            BuildingId = buildingId;
            Name = name;
            Description = description;
            Category = category;
            Width = width;
            Height = height;
            BuildCostCoins = buildCostCoins;
            ConstructionTimeSeconds = constructionTimeSeconds;
            UnlockLevel = unlockLevel;
            XpReward = xpReward;
            PopulationCapacity = populationCapacity;
            StorageCapacityBonus = storageCapacityBonus;
        }
    }

    public static class BuildingLibrary
    {
        private static readonly Dictionary<string, BuildingConfig> _buildings = new Dictionary<string, BuildingConfig>
        {
            {
                "small_house",
                new BuildingConfig("small_house", "Small House", "Increases town population capacity.", BuildingCategory.Residential, 2, 2, 100, 15f, unlockLevel: 1, xpReward: 15, populationCapacity: 5)
            },
            {
                "family_house",
                new BuildingConfig("family_house", "Family House", "Provides higher population capacity for growing towns.", BuildingCategory.Residential, 3, 2, 300, 45f, unlockLevel: 2, xpReward: 35, populationCapacity: 12)
            },
            {
                "barn",
                new BuildingConfig("barn", "Barn", "Increases inventory storage capacity.", BuildingCategory.Storage, 3, 3, 250, 30f, unlockLevel: 1, xpReward: 25, storageCapacityBonus: 20)
            },
            {
                "town_hall",
                new BuildingConfig("town_hall", "Town Hall", "Main administration building of the town.", BuildingCategory.Community, 3, 3, 500, 60f, unlockLevel: 1, xpReward: 50)
            },
            {
                "feed_mill",
                new BuildingConfig("feed_mill", "Feed Mill", "Converts crops into animal feed.", BuildingCategory.Production, 3, 3, 150, 20f, unlockLevel: 1, xpReward: 20)
            },
            {
                "bakery",
                new BuildingConfig("bakery", "Bakery", "Bakes fresh flour and bread from crops.", BuildingCategory.Production, 3, 3, 200, 30f, unlockLevel: 2, xpReward: 25)
            },
            {
                "sugar_mill",
                new BuildingConfig("sugar_mill", "Sugar Mill", "Processes sugarcane into sugar.", BuildingCategory.Production, 3, 3, 300, 40f, unlockLevel: 3, xpReward: 30)
            },
            {
                "dairy_factory",
                new BuildingConfig("dairy_factory", "Dairy Factory", "Produces fresh milk using animal feed.", BuildingCategory.Production, 3, 3, 400, 50f, unlockLevel: 4, xpReward: 40)
            },
            {
                "tree",
                new BuildingConfig("tree", "Pine Tree", "A nice decorative pine tree.", BuildingCategory.Decoration, 1, 1, 20, 0f, unlockLevel: 1, xpReward: 2)
            },
            {
                "flower_bed",
                new BuildingConfig("flower_bed", "Flower Bed", "Colorful flower bed decoration.", BuildingCategory.Decoration, 1, 1, 30, 0f, unlockLevel: 1, xpReward: 3)
            },
            {
                "small_fountain",
                new BuildingConfig("small_fountain", "Small Fountain", "A peaceful water fountain.", BuildingCategory.Decoration, 2, 2, 150, 10f, unlockLevel: 2, xpReward: 15)
            }
        };

        public static BuildingConfig GetBuilding(string buildingId)
        {
            return _buildings.TryGetValue(buildingId, out var config) ? config : null;
        }

        public static List<BuildingConfig> GetAllBuildings()
        {
            return new List<BuildingConfig>(_buildings.Values);
        }

        public static List<BuildingConfig> GetBuildingsByCategory(BuildingCategory category)
        {
            var result = new List<BuildingConfig>();
            foreach (var b in _buildings.Values)
            {
                if (b.Category == category) result.Add(b);
            }
            return result;
        }
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

        public RecipeIngredient() { }

        public RecipeIngredient(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }

    [Serializable]
    public class RecipeConfig
    {
        public string RecipeId;
        public string Name;
        public string BuildingId;
        public string OutputItemId;
        public int OutputQuantity = 1;
        public float ProductionTimeSeconds = 15f;
        public int XpReward = 10;
        public int UnlockLevel = 1;
        public List<RecipeIngredient> Ingredients = new List<RecipeIngredient>();

        public RecipeConfig() { }

        public RecipeConfig(
            string recipeId,
            string name,
            string buildingId,
            string outputItemId,
            int outputQuantity,
            float productionTimeSeconds,
            int xpReward,
            int unlockLevel,
            List<RecipeIngredient> ingredients)
        {
            RecipeId = recipeId;
            Name = name;
            BuildingId = buildingId;
            OutputItemId = outputItemId;
            OutputQuantity = outputQuantity;
            ProductionTimeSeconds = productionTimeSeconds;
            XpReward = xpReward;
            UnlockLevel = unlockLevel;
            Ingredients = ingredients ?? new List<RecipeIngredient>();
        }
    }

    public static class RecipeLibrary
    {
        private static readonly Dictionary<string, RecipeConfig> _recipes = new Dictionary<string, RecipeConfig>
        {
            {
                "recipe_animal_feed",
                new RecipeConfig(
                    "recipe_animal_feed",
                    "Animal Feed",
                    "feed_mill",
                    "item_animal_feed",
                    outputQuantity: 1,
                    productionTimeSeconds: 15f,
                    xpReward: 8,
                    unlockLevel: 1,
                    new List<RecipeIngredient> { new RecipeIngredient("crop_wheat", 2) }
                )
            },
            {
                "recipe_flour",
                new RecipeConfig(
                    "recipe_flour",
                    "Flour",
                    "bakery",
                    "item_flour",
                    outputQuantity: 1,
                    productionTimeSeconds: 20f,
                    xpReward: 10,
                    unlockLevel: 2,
                    new List<RecipeIngredient> { new RecipeIngredient("crop_wheat", 2) }
                )
            },
            {
                "recipe_bread",
                new RecipeConfig(
                    "recipe_bread",
                    "Bread",
                    "bakery",
                    "item_bread",
                    outputQuantity: 1,
                    productionTimeSeconds: 30f,
                    xpReward: 18,
                    unlockLevel: 2,
                    new List<RecipeIngredient> { new RecipeIngredient("item_flour", 1), new RecipeIngredient("item_sugar", 1) }
                )
            },
            {
                "recipe_sugar",
                new RecipeConfig(
                    "recipe_sugar",
                    "Sugar",
                    "sugar_mill",
                    "item_sugar",
                    outputQuantity: 1,
                    productionTimeSeconds: 25f,
                    xpReward: 15,
                    unlockLevel: 3,
                    new List<RecipeIngredient> { new RecipeIngredient("crop_sugarcane", 2) }
                )
            },
            {
                "recipe_milk",
                new RecipeConfig(
                    "recipe_milk",
                    "Milk",
                    "dairy_factory",
                    "item_milk",
                    outputQuantity: 1,
                    productionTimeSeconds: 35f,
                    xpReward: 20,
                    unlockLevel: 4,
                    new List<RecipeIngredient> { new RecipeIngredient("item_animal_feed", 2) }
                )
            }
        };

        public static RecipeConfig GetRecipe(string recipeId)
        {
            return _recipes.TryGetValue(recipeId, out var config) ? config : null;
        }

        public static List<RecipeConfig> GetRecipesForBuilding(string buildingId)
        {
            var list = new List<RecipeConfig>();
            foreach (var r in _recipes.Values)
            {
                if (r.BuildingId == buildingId) list.Add(r);
            }
            return list;
        }

        public static List<RecipeConfig> GetAllRecipes()
        {
            return new List<RecipeConfig>(_recipes.Values);
        }
    }
}
