using System.Collections.Generic;
using NUnit.Framework;
using Game.Player;
using Game.Economy;
using Game.Inventory;
using Game.World;
using Game.Save;

namespace Game.Tests
{
    [TestFixture]
    public class GameCoreTests
    {
        private PlayerProfile _profile;
        private EconomyManager _economyManager;
        private InventoryManager _inventoryManager;

        [SetUp]
        public void SetUp()
        {
            _profile = new PlayerProfile
            {
                Level = 1,
                CurrentXP = 0,
                Coins = 500,
                Gems = 20
            };

            _economyManager = new EconomyManager(_profile);
            _inventoryManager = new InventoryManager(50);
        }

        [Test]
        public void PlayerProfile_AddXP_LevelsUpCorrectly()
        {
            // Level 1 requires 100 XP
            _profile.AddXP(150);

            Assert.AreEqual(2, _profile.Level);
            Assert.AreEqual(50, _profile.CurrentXP);
            Assert.AreEqual(200, _profile.RequiredXPForNextLevel);
        }

        [Test]
        public void EconomyManager_SpendCoins_ValidAndInvalid()
        {
            Assert.IsTrue(_economyManager.CanAffordCoins(300));
            Assert.IsTrue(_economyManager.SpendCoins(300));
            Assert.AreEqual(200, _profile.Coins);

            Assert.IsFalse(_economyManager.CanAffordCoins(500));
            Assert.IsFalse(_economyManager.SpendCoins(500));
            Assert.AreEqual(200, _profile.Coins);
        }

        [Test]
        public void EconomyManager_EarnCoinsAndGems_IncreasesBalance()
        {
            _economyManager.EarnCoins(100);
            _economyManager.EarnGems(5);

            Assert.AreEqual(600, _profile.Coins);
            Assert.AreEqual(25, _profile.Gems);
        }

        [Test]
        public void InventoryManager_AddAndRemoveItem_UpdatesQuantityAndCapacity()
        {
            Assert.IsTrue(_inventoryManager.AddItem("wheat", "Wheat", ItemType.Crop, 20));
            Assert.AreEqual(20, _inventoryManager.GetQuantity("wheat"));
            Assert.AreEqual(20, _inventoryManager.CurrentCount);

            Assert.IsTrue(_inventoryManager.RemoveItem("wheat", 5));
            Assert.AreEqual(15, _inventoryManager.GetQuantity("wheat"));
            Assert.AreEqual(15, _inventoryManager.CurrentCount);
        }

        [Test]
        public void InventoryManager_ExceedCapacity_RejectsAddition()
        {
            Assert.IsFalse(_inventoryManager.CanAddItem(60));
            Assert.IsFalse(_inventoryManager.AddItem("corn", "Corn", ItemType.Crop, 60));
            Assert.AreEqual(0, _inventoryManager.GetQuantity("corn"));
        }

        [Test]
        public void WorldGrid_PlacementValidation()
        {
            var grid = new WorldGrid(10, 10);

            // Origin (1,1), size 2x2 on empty grass
            Assert.IsTrue(grid.CanPlaceObject(new UnityEngine.Vector2Int(1, 1), 2, 2));
            Assert.IsTrue(grid.PlaceObject(new UnityEngine.Vector2Int(1, 1), 2, 2, "bldg_1"));

            // Tile is now occupied
            Assert.IsFalse(grid.CanPlaceObject(new UnityEngine.Vector2Int(1, 1), 1, 1));
            Assert.IsFalse(grid.CanPlaceObject(new UnityEngine.Vector2Int(2, 2), 1, 1));

            // Out of bounds placement
            Assert.IsFalse(grid.CanPlaceObject(new UnityEngine.Vector2Int(9, 9), 2, 2));
        }

        [Test]
        public void SaveSystem_SaveAndLoad_CorruptedSaveRecovery()
        {
            var storage = new MockStorage();
            var saveSystem = new LocalSaveSystem("save.json", storage);

            var initialSave = new SaveData
            {
                Version = 1,
                PlayerProfile = new PlayerProfile { Level = 5, Coins = 1200, Gems = 50 },
                InventoryItems = new List<InventoryItem>
                {
                    new InventoryItem("wheat", "Wheat", ItemType.Crop, 10)
                }
            };

            saveSystem.Save(initialSave);
            Assert.IsTrue(storage.Exists("save.json"));

            var loadedSave = saveSystem.Load();
            Assert.AreEqual(5, loadedSave.PlayerProfile.Level);
            Assert.AreEqual(1200, loadedSave.PlayerProfile.Coins);
            Assert.AreEqual(1, loadedSave.InventoryItems.Count);

            // Test corrupted data recovery
            storage.WriteAllText("save.json", "INVALID_CORRUPTED_JSON_DATA");
            var recoveredSave = saveSystem.Load();
            Assert.IsNotNull(recoveredSave);
            Assert.AreEqual(1, recoveredSave.PlayerProfile.Level);
            Assert.AreEqual(500, recoveredSave.PlayerProfile.Coins);
        }

        private class MockStorage : ISaveStorage
        {
            private readonly Dictionary<string, string> _files = new Dictionary<string, string>();

            public void WriteAllText(string path, string contents) => _files[path] = contents;
            public string ReadAllText(string path) => _files.TryGetValue(path, out var contents) ? contents : string.Empty;
            public bool Exists(string path) => _files.ContainsKey(path);
            public void Delete(string path) => _files.Remove(path);
        }
    }
}
