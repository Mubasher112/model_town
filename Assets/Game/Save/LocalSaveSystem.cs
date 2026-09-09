using System;
using System.Collections.Generic;
using Game.Inventory;
using Game.Player;

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
    public class SaveData
    {
        public int Version = 5;
        public long Timestamp;
        public PlayerProfile PlayerProfile = new PlayerProfile();
        public List<InventoryItem> InventoryItems = new List<InventoryItem>();
        public List<SavedPlacedObject> PlacedObjects = new List<SavedPlacedObject>();
        public List<SavedRoadTile> RoadTiles = new List<SavedRoadTile>();
        public List<SavedField> Fields = new List<SavedField>();
        public List<SavedBuilding> Buildings = new List<SavedBuilding>();
        public List<SavedProductionBuilding> ProductionBuildings = new List<SavedProductionBuilding>();
        public List<string> UnlockedZoneIds = new List<string>();
        public int MapWidth = 30;
        public int MapHeight = 30;
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
        public const int CurrentSaveVersion = 5;
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
                }
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
