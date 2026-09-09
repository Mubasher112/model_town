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
        public int Width;
        public int Height;
    }

    [Serializable]
    public class SaveData
    {
        public int Version = 1;
        public long Timestamp;
        public PlayerProfile PlayerProfile = new PlayerProfile();
        public List<InventoryItem> InventoryItems = new List<InventoryItem>();
        public List<SavedPlacedObject> PlacedObjects = new List<SavedPlacedObject>();
        public int UnlockedLandExpansions = 1;
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
        public const int CurrentSaveVersion = 1;
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

                // Handle version migration if needed in the future
                if (data.Version < CurrentSaveVersion)
                {
                    MigrateSaveData(data);
                }

                return data;
            }
            catch
            {
                // Fallback / recovery from corrupted save
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
                }
            };
            Save(data);
            return data;
        }

        private void MigrateSaveData(SaveData data)
        {
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
