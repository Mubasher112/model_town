using System;
using System.Collections.Generic;
using Game.Inventory;
using Game.Player;
using Game.Data;
using Game.Services;
using Game.Adventure;

namespace Game.Save
{
    [Serializable]
    public class SavedPlacedObject
    {
        public string ObjectId;
        public string ObjectTypeId;
        public int X;
        public int Y;
        public int BaseWidth = 1;
        public int BaseHeight = 1;
        public int RotationDegrees = 0;
    }

    [Serializable]
    public class SavedRoadTile
    {
        public int X;
        public int Y;

        public SavedRoadTile() { }

        public SavedRoadTile(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public class SavedField
    {
        public string FieldId;
        public int X;
        public int Y;
        public int Width = 1;
        public int Height = 1;
        public int State;
        public string CurrentCropId;
        public long PlantedUtcTicks;
    }

    [Serializable]
    public class SavedBuilding
    {
        public string InstanceId;
        public string BuildingId;
        public int X;
        public int Y;
        public int BaseWidth = 1;
        public int BaseHeight = 1;
        public int RotationDegrees = 0;
        public int Level = 1;
        public int State;
        public long ConstructionStartUtcTicks;
        public long UpgradeStartUtcTicks;
        public bool CompletionXpAwarded;
    }

    [Serializable]
    public class SavedProductionJob
    {
        public string JobId;
        public string RecipeId;
        public long StartUtcTicks;
        public int State;
        public bool XpAwarded;
    }

    [Serializable]
    public class SavedProductionBuilding
    {
        public string BuildingInstanceId;
        public string BuildingId;
        public int QueueCapacity = 2;
        public List<SavedProductionJob> JobsQueue = new List<SavedProductionJob>();
    }

    [Serializable]
    public class SavedOrderRequirement
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public class SavedOrder
    {
        public string OrderId;
        public string CustomerId;
        public int Type;
        public List<SavedOrderRequirement> Requirements = new List<SavedOrderRequirement>();
        public long RewardCoins;
        public int RewardXp;
        public int State;
        public long CreationUtcTicks;
        public long ExpirationUtcTicks;
    }

    [Serializable]
    public class SavedOrderHistory
    {
        public string OrderId;
        public string CustomerId;
        public long CoinsEarned;
        public int XpEarned;
        public long CompletionUtcTicks;
    }

    [Serializable]
    public class SavedResident
    {
        public string ResidentId;
        public string ResidentTypeId;
        public string DisplayName;
        public string AssignedHouseInstanceId;
        public int State;
        public long CreationUtcTicks;
    }

    [Serializable]
    public class SavedAdventureCell
    {
        public int X;
        public int Y;

        public SavedAdventureCell() { }

        public SavedAdventureCell(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [Serializable]
    public class SavedAdventureNode
    {
        public string NodeInstanceId;
        public string NodeDefId;
        public int X;
        public int Y;
        public int State;
        public int CurrentQuantity;
        public int MaxQuantity;
        public long GatheringStartUtcTicks;
        public long RespawnStartUtcTicks;
        public bool IsCleared;
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 11;
        public long Timestamp;
        public PlayerProfile PlayerProfile = new PlayerProfile();
        public List<InventoryItem> InventoryItems = new List<InventoryItem>();
        public List<SavedPlacedObject> PlacedObjects = new List<SavedPlacedObject>();
        public List<SavedRoadTile> RoadTiles = new List<SavedRoadTile>();
        public List<SavedField> Fields = new List<SavedField>();
        public List<SavedBuilding> Buildings = new List<SavedBuilding>();
        public List<SavedProductionBuilding> ProductionBuildings = new List<SavedProductionBuilding>();
        public List<SavedOrder> ActiveOrders = new List<SavedOrder>();
        public List<SavedOrderHistory> OrderHistory = new List<SavedOrderHistory>();
        public List<SavedResident> Residents = new List<SavedResident>();
        public List<string> UnlockedZoneIds = new List<string>();
        public int MapWidth = 30;
        public int MapHeight = 30;

        // Adventure Persistence Fields (v9)
        public bool IsAdventureUnlocked = false;
        public int AdventurePlayerX = 12;
        public int AdventurePlayerY = 2;
        public EnergyState EnergyState = new EnergyState();
        public List<ToolInstance> ToolInstances = new List<ToolInstance>();
        public List<SavedAdventureCell> DiscoveredAdventureCells = new List<SavedAdventureCell>();
        public List<SavedAdventureNode> AdventureNodes = new List<SavedAdventureNode>();
        public List<string> DiscoveredSpecialLocations = new List<string>();

        // Social Persistence Fields (v10)
        public SocialProfile LocalSocialProfile = new SocialProfile();
        public List<FriendRelationship> CachedFriends = new List<FriendRelationship>();
        public List<string> BlockedPlayerIds = new List<string>();
        public List<string> ClaimedGiftIds = new List<string>();
        public List<string> AppreciatedPlayerIds = new List<string>();

        // Market Persistence Fields (v11)
        public List<MarketListing> MarketListings = new List<MarketListing>();
        public List<MarketTransaction> MarketHistory = new List<MarketTransaction>();
    }

    public interface ISaveStorage
    {
        void WriteAllText(string path, string contents);
        string ReadAllText(string path);
        bool Exists(string path);
        void Delete(string path);
    }

    public class LocalSaveSystem
    {
        public const int CurrentSaveVersion = 11;
        private readonly string _saveFilePath;
        private readonly ISaveStorage _storage;

        public LocalSaveSystem(string saveFilePath, ISaveStorage storage = null)
        {
            _saveFilePath = saveFilePath;
            _storage = storage ?? new DefaultFileStorage();
        }

        public bool Save(SaveData data)
        {
            try
            {
                data.Version = CurrentSaveVersion;
                data.Timestamp = DateTime.UtcNow.Ticks;
                string json = SimpleJsonSerializer.ToJson(data);
                _storage.WriteAllText(_saveFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public SaveData Load()
        {
            try
            {
                if (!_storage.Exists(_saveFilePath))
                {
                    return CreateNewSave();
                }

                string json = _storage.ReadAllText(_saveFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return CreateNewSave();
                }

                SaveData data = SimpleJsonSerializer.FromJson<SaveData>(json);
                if (data == null || data.PlayerProfile == null)
                {
                    return CreateNewSave();
                }

                if (data.Version < CurrentSaveVersion)
                {
                    MigrateSaveData(data);
                }

                return data;
            }
            catch
            {
                return CreateNewSave();
            }
        }

        public SaveData CreateNewSave()
        {
            var data = new SaveData
            {
                Version = CurrentSaveVersion,
                Timestamp = DateTime.UtcNow.Ticks,
                PlayerProfile = new PlayerProfile
                {
                    PlayerName = "Mayor",
                    Level = 1,
                    CurrentXP = 0,
                    Coins = 500,
                    Gems = 20
                },
                UnlockedZoneIds = new List<string> { "zone_start" },
                MapWidth = 30,
                MapHeight = 30,
                InventoryItems = new List<InventoryItem>
                {
                    new InventoryItem("seed_wheat", "Wheat Seeds", ItemType.Seed, 10),
                    new InventoryItem("seed_corn", "Corn Seeds", ItemType.Seed, 5),
                    new InventoryItem("crop_wheat", "Wheat", ItemType.Crop, 10),
                    new InventoryItem("crop_sugarcane", "Sugarcane", ItemType.Crop, 10),
                    new InventoryItem("wood", "Wood", ItemType.RawMaterial, 20),
                    new InventoryItem("stone", "Stone", ItemType.RawMaterial, 10)
                },
                IsAdventureUnlocked = false,
                AdventurePlayerX = 12,
                AdventurePlayerY = 2,
                EnergyState = new EnergyState(20, DateTime.UtcNow.Ticks),
                ToolInstances = new List<ToolInstance>
                {
                    new ToolInstance("tool_pickaxe", 30, 30),
                    new ToolInstance("tool_axe", 30, 30),
                    new ToolInstance("tool_shovel", 25, 25)
                },
                DiscoveredAdventureCells = new List<SavedAdventureCell>(),
                AdventureNodes = new List<SavedAdventureNode>(),
                DiscoveredSpecialLocations = new List<string>(),
                LocalSocialProfile = new SocialProfile("p_local_me", "Mayor's Valley", 1, 2, 100),
                CachedFriends = new List<FriendRelationship>(),
                BlockedPlayerIds = new List<string>(),
                ClaimedGiftIds = new List<string>(),
                AppreciatedPlayerIds = new List<string>(),
                MarketListings = new List<MarketListing>(),
                MarketHistory = new List<MarketTransaction>()
            };
            Save(data);
            return data;
        }

        private void MigrateSaveData(SaveData data)
        {
            if (data.Version < 2)
            {
                if (data.UnlockedZoneIds == null || data.UnlockedZoneIds.Count == 0)
                {
                    data.UnlockedZoneIds = new List<string> { "zone_start" };
                }
                if (data.MapWidth <= 0) data.MapWidth = 30;
                if (data.MapHeight <= 0) data.MapHeight = 30;
                if (data.RoadTiles == null) data.RoadTiles = new List<SavedRoadTile>();
            }

            if (data.Version < 3)
            {
                if (data.Fields == null) data.Fields = new List<SavedField>();
            }

            if (data.Version < 4)
            {
                if (data.Buildings == null) data.Buildings = new List<SavedBuilding>();
            }

            if (data.Version < 5)
            {
                if (data.ProductionBuildings == null) data.ProductionBuildings = new List<SavedProductionBuilding>();
            }

            if (data.Version < 6)
            {
                if (data.ActiveOrders == null) data.ActiveOrders = new List<SavedOrder>();
                if (data.OrderHistory == null) data.OrderHistory = new List<SavedOrderHistory>();
            }

            if (data.Version < 7)
            {
                if (data.Residents == null) data.Residents = new List<SavedResident>();
            }

            if (data.Version < 8)
            {
            }

            if (data.Version < 9)
            {
                data.IsAdventureUnlocked = false;
                if (data.EnergyState == null) data.EnergyState = new EnergyState(20, DateTime.UtcNow.Ticks);
                if (data.ToolInstances == null)
                {
                    data.ToolInstances = new List<ToolInstance>
                    {
                        new ToolInstance("tool_pickaxe", 30, 30),
                        new ToolInstance("tool_axe", 30, 30),
                        new ToolInstance("tool_shovel", 25, 25)
                    };
                }
                if (data.DiscoveredAdventureCells == null) data.DiscoveredAdventureCells = new List<SavedAdventureCell>();
                if (data.AdventureNodes == null) data.AdventureNodes = new List<SavedAdventureNode>();
                if (data.DiscoveredSpecialLocations == null) data.DiscoveredSpecialLocations = new List<string>();
            }

            if (data.Version < 10)
            {
                if (data.LocalSocialProfile == null) data.LocalSocialProfile = new SocialProfile("p_local_me", "Mayor's Valley", 1, 2, 100);
                if (data.CachedFriends == null) data.CachedFriends = new List<FriendRelationship>();
                if (data.BlockedPlayerIds == null) data.BlockedPlayerIds = new List<string>();
                if (data.ClaimedGiftIds == null) data.ClaimedGiftIds = new List<string>();
                if (data.AppreciatedPlayerIds == null) data.AppreciatedPlayerIds = new List<string>();
            }

            if (data.Version < 11)
            {
                if (data.MarketListings == null) data.MarketListings = new List<MarketListing>();
                if (data.MarketHistory == null) data.MarketHistory = new List<MarketTransaction>();
            }

            data.Version = CurrentSaveVersion;
        }

        private class DefaultFileStorage : ISaveStorage
        {
            public void WriteAllText(string path, string contents) => System.IO.File.WriteAllText(path, contents);
            public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
            public bool Exists(string path) => System.IO.File.Exists(path);
            public void Delete(string path) => System.IO.File.Delete(path);
        }
    }

    public static class SimpleJsonSerializer
    {
        public static string ToJson<T>(T obj)
        {
            return UnityEngine.JsonUtility.ToJson(obj, true);
        }

        public static T FromJson<T>(string json)
        {
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }
    }
}
