using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
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

        #region World & Grid System Tests
        [Test]
        public void WorldGrid_CoordinateConversions_GridToWorldToGrid()
        {
            var grid = new WorldGrid(30, 30);
            Vector2Int originalGridPos = new Vector2Int(12, 18);

            Vector3 worldPos = grid.GridToWorld(originalGridPos);
            Vector2Int convertedGridPos = grid.WorldToGrid(worldPos);

            Assert.AreEqual(originalGridPos.x, convertedGridPos.x);
            Assert.AreEqual(originalGridPos.y, convertedGridPos.y);
        }

        [Test]
        public void WorldGrid_CoordinateConversions_ScreenToGrid()
        {
            var grid = new WorldGrid(30, 30);
            Vector3 cameraPos = new Vector3(0, 0, 0);
            float zoom = 10f;

            Vector3 centerScreenPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Vector2Int gridPos = grid.ScreenToGrid(centerScreenPos, cameraPos, zoom);

            Vector3 worldPos = grid.GridToWorld(gridPos);
            Assert.LessOrEqual(Mathf.Abs(worldPos.x - cameraPos.x), 1.0f);
        }

        [Test]
        public void ObjectPlacement_FootprintRotationAndValidation()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);
            expansionManager.UnlockAllZonesDev(); // Unlock all tiles for clean test

            var placementManager = new ObjectPlacementManager(grid);
            var footprint = new ObjectFootprint(3, 2); // 3x2 base

            // 0 Deg Rotation: Effective 3x2
            Vector2Int origin = new Vector2Int(10, 10);
            Assert.IsTrue(placementManager.TryPlaceObject("bldg_1", "factory", origin, footprint, RotationAngle.Deg0, out var res0));
            Assert.AreEqual(PlacementResult.Valid, res0);

            // Cannot place overlapping object
            Assert.IsFalse(placementManager.TryPlaceObject("bldg_2", "factory", origin, footprint, RotationAngle.Deg0, out var resOverlap));
            Assert.AreEqual(PlacementResult.InvalidOccupied, resOverlap);

            // Remove object
            Assert.IsTrue(placementManager.RemoveObject("bldg_1"));

            // 90 Deg Rotation: Effective 2x3
            Assert.IsTrue(placementManager.TryPlaceObject("bldg_1", "factory", origin, footprint, RotationAngle.Deg90, out var res90));
            Assert.AreEqual(PlacementResult.Valid, res90);
        }

        [Test]
        public void ObjectPlacement_LockedAndWaterRejection()
        {
            var grid = new WorldGrid(30, 30); // Borders are water, outer tiles locked
            var placementManager = new ObjectPlacementManager(grid);
            var footprint = new ObjectFootprint(1, 1);

            // Water tile at border (0,0)
            Assert.AreEqual(PlacementResult.InvalidWater, placementManager.CheckPlacement(new Vector2Int(0, 0), footprint, RotationAngle.Deg0));

            // Locked tile at (2,2)
            Assert.AreEqual(PlacementResult.InvalidLocked, placementManager.CheckPlacement(new Vector2Int(2, 2), footprint, RotationAngle.Deg0));
        }

        [Test]
        public void RoadManager_PlacementAndRemoval()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);
            expansionManager.UnlockAllZonesDev();

            var roadManager = new RoadManager(grid);
            Vector2Int pos = new Vector2Int(10, 10);

            Assert.IsTrue(roadManager.CanPlaceRoad(pos));
            Assert.IsTrue(roadManager.PlaceRoad(pos));
            Assert.IsTrue(roadManager.IsRoad(pos));
            Assert.AreEqual(TileType.Road, grid.GetTile(pos).Type);

            Assert.IsTrue(roadManager.RemoveRoad(pos));
            Assert.IsFalse(roadManager.IsRoad(pos));
            Assert.AreEqual(TileType.Grass, grid.GetTile(pos).Type);
        }

        [Test]
        public void LandExpansionManager_UnlockingZones()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);

            Vector2Int northZoneCoord = new Vector2Int(10, 26);
            Assert.IsTrue(grid.GetTile(northZoneCoord).IsLocked);

            Assert.IsTrue(expansionManager.UnlockZone("zone_north"));
            Assert.IsFalse(grid.GetTile(northZoneCoord).IsLocked);
        }
        #endregion

        [Test]
        public void SaveSystem_SaveAndLoad_WorldPersistence()
        {
            var storage = new MockStorage();
            var saveSystem = new LocalSaveSystem("save_v2.json", storage);

            var initialSave = new SaveData
            {
                Version = 2,
                PlayerProfile = new PlayerProfile { Level = 5, Coins = 1200, Gems = 50 },
                UnlockedZoneIds = new List<string> { "zone_start", "zone_north" },
                RoadTiles = new List<SavedRoadTile> { new SavedRoadTile(10, 10), new SavedRoadTile(10, 11) },
                PlacedObjects = new List<SavedPlacedObject>
                {
                    new SavedPlacedObject { ObjectId = "bldg_1", ObjectTypeId = "house", X = 12, Y = 12, BaseWidth = 2, BaseHeight = 2, RotationDegrees = 90 }
                }
            };

            saveSystem.Save(initialSave);
            Assert.IsTrue(storage.Exists("save_v2.json"));

            var loadedSave = saveSystem.Load();
            Assert.AreEqual(2, loadedSave.Version);
            Assert.AreEqual(5, loadedSave.PlayerProfile.Level);
            Assert.AreEqual(2, loadedSave.UnlockedZoneIds.Count);
            Assert.AreEqual(2, loadedSave.RoadTiles.Count);
            Assert.AreEqual(1, loadedSave.PlacedObjects.Count);
            Assert.AreEqual("bldg_1", loadedSave.PlacedObjects[0].ObjectId);
            Assert.AreEqual(90, loadedSave.PlacedObjects[0].RotationDegrees);
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
