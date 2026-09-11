using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Player;
using Game.Economy;
using Game.Inventory;
using Game.World;
using Game.Farming;
using Game.Buildings;
using Game.Production;
using Game.Orders;
using Game.Residents;
using Game.Data;
using Game.Services;
using Game.Save;
using Game.Adventure;
using Game.Social;
using Game.Quests;

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

            Vector3 centerScreenPos = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Vector2Int gridPos = grid.ScreenToGrid(centerScreenPos, cameraPos, 10f);

            Vector3 worldPos = grid.GridToWorld(gridPos);
            Assert.LessOrEqual(Mathf.Abs(worldPos.x - cameraPos.x), 1.0f);
        }

        [Test]
        public void ObjectPlacement_FootprintRotationAndValidation()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);
            expansionManager.UnlockAllZonesDev();

            var placementManager = new ObjectPlacementManager(grid);
            var footprint = new ObjectFootprint(3, 2);

            Vector2Int origin = new Vector2Int(10, 10);
            Assert.IsTrue(placementManager.TryPlaceObject("bldg_1", "factory", origin, footprint, RotationAngle.Deg0, out var res0));
            Assert.AreEqual(PlacementResult.Valid, res0);

            Assert.IsFalse(placementManager.TryPlaceObject("bldg_2", "factory", origin, footprint, RotationAngle.Deg0, out var resOverlap));
            Assert.AreEqual(PlacementResult.InvalidOccupied, resOverlap);

            Assert.IsTrue(placementManager.RemoveObject("bldg_1"));

            Assert.IsTrue(placementManager.TryPlaceObject("bldg_1", "factory", origin, footprint, RotationAngle.Deg90, out var res90));
            Assert.AreEqual(PlacementResult.Valid, res90);
        }

        [Test]
        public void ObjectPlacement_LockedAndWaterRejection()
        {
            var grid = new WorldGrid(30, 30);
            var placementManager = new ObjectPlacementManager(grid);
            var footprint = new ObjectFootprint(1, 1);

            Assert.AreEqual(PlacementResult.InvalidWater, placementManager.CheckPlacement(new Vector2Int(0, 0), footprint, RotationAngle.Deg0));
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

            Assert.IsTrue(expansionManager.UnlockZone("expansion_01"));
            Assert.IsFalse(grid.GetTile(northZoneCoord).IsLocked);
        }
        #endregion

        #region Land Expansion, Roads & Accessibility System Tests
        [Test]
        public void LandExpansion_TryPurchaseExpansion_ValidatesRequirementsAndDeductsCoins()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);

            _profile.Level = 5;
            _profile.Coins = 1000;

            var result = expansionManager.TryPurchaseExpansion("expansion_01", _profile, _economyManager, currentPopulation: 0);

            Assert.AreEqual(ExpansionOperationResult.Success, result);
            Assert.AreEqual(500, _profile.Coins);
            Assert.IsFalse(grid.GetTile(new Vector2Int(10, 26)).IsLocked);

            var duplicateResult = expansionManager.TryPurchaseExpansion("expansion_01", _profile, _economyManager, currentPopulation: 0);
            Assert.AreEqual(ExpansionOperationResult.AlreadyUnlocked, duplicateResult);
        }

        [Test]
        public void LandExpansion_TryPurchaseExpansion_RejectsInsufficientCoinsOrLevel()
        {
            var grid = new WorldGrid(30, 30);
            var expansionManager = new LandExpansionManager(grid);

            _profile.Level = 1;
            _profile.Coins = 1000;

            var result = expansionManager.TryPurchaseExpansion("expansion_01", _profile, _economyManager, currentPopulation: 0);
            Assert.AreEqual(ExpansionOperationResult.LevelRequirementNotMet, result);
            Assert.AreEqual(1000, _profile.Coins);
        }

        [Test]
        public void RoadManager_AutoConnectivityTypes()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var roadManager = new RoadManager(grid);

            Vector2Int center = new Vector2Int(10, 10);
            roadManager.PlaceRoad(center);

            Assert.AreEqual(RoadConnectionType.Isolated, roadManager.GetRoadConnectionType(center));

            roadManager.PlaceRoad(center + new Vector2Int(0, 1));
            Assert.AreEqual(RoadConnectionType.DeadEnd, roadManager.GetRoadConnectionType(center));

            roadManager.PlaceRoad(center + new Vector2Int(0, -1));
            Assert.AreEqual(RoadConnectionType.Straight, roadManager.GetRoadConnectionType(center));

            roadManager.PlaceRoad(center + new Vector2Int(1, 0));
            Assert.AreEqual(RoadConnectionType.TJunction, roadManager.GetRoadConnectionType(center));

            roadManager.PlaceRoad(center + new Vector2Int(-1, 0));
            Assert.AreEqual(RoadConnectionType.Cross, roadManager.GetRoadConnectionType(center));
        }

        [Test]
        public void BuildingAccessibilityService_UpdatesOnRoadChanges()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var roadManager = new RoadManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);
            var accessibilityService = new BuildingAccessibilityService(buildingManager, roadManager);

            buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var house);
            buildingManager.DevInstantCompleteConstruction(house.InstanceId);

            Assert.IsFalse(accessibilityService.IsBuildingAccessible(house));

            roadManager.PlaceRoad(new Vector2Int(10, 9));
            Assert.IsTrue(accessibilityService.IsBuildingAccessible(house));

            roadManager.RemoveRoad(new Vector2Int(10, 9));
            Assert.IsFalse(accessibilityService.IsBuildingAccessible(house));
        }
        #endregion

        #region Farming System Tests
        [Test]
        public void CropLibrary_LoadsConfigurationsCorrectly()
        {
            var wheat = CropLibrary.GetCrop("wheat");
            Assert.IsNotNull(wheat);
            Assert.AreEqual("Wheat", wheat.Name);
            Assert.AreEqual(10f, wheat.GrowthTimeSeconds);
            Assert.AreEqual("seed_wheat", wheat.SeedItemId);
            Assert.AreEqual("crop_wheat", wheat.HarvestItemId);

            var corn = CropLibrary.GetCrop("corn");
            Assert.IsNotNull(corn);
            Assert.AreEqual(2, corn.UnlockLevel);
        }

        [Test]
        public void FarmManager_PlantCrop_ConsumesSeedAndStartsGrowth()
        {
            var timeService = new StandardGameTimeService();
            var farmManager = new FarmManager(_inventoryManager, _profile, timeService);

            var field = new FieldInstance("f1", new Vector2Int(10, 10));
            farmManager.RegisterField(field);

            _inventoryManager.AddItem("seed_wheat", "Wheat Seeds", ItemType.Seed, 2);

            var result = farmManager.PlantCrop("f1", "wheat");
            Assert.AreEqual(FarmingOperationResult.Success, result);
            Assert.AreEqual(FieldState.Planted, field.State);
            Assert.AreEqual(1, _inventoryManager.GetQuantity("seed_wheat"));
        }

        [Test]
        public void FarmManager_PlantCrop_RejectsMissingSeedAndLockedCrop()
        {
            var timeService = new StandardGameTimeService();
            var farmManager = new FarmManager(_inventoryManager, _profile, timeService);

            var field1 = new FieldInstance("f1", new Vector2Int(10, 10));
            farmManager.RegisterField(field1);

            var lockedResult = farmManager.PlantCrop("f1", "tomato");
            Assert.AreEqual(FarmingOperationResult.CropLocked, lockedResult);

            var noSeedResult = farmManager.PlantCrop("f1", "wheat");
            Assert.AreEqual(FarmingOperationResult.MissingSeed, noSeedResult);
        }

        [Test]
        public void FieldInstance_OfflineGrowthCalculation()
        {
            long startTicks = System.DateTime.UtcNow.Ticks;
            var wheat = CropLibrary.GetCrop("wheat");

            var field = new FieldInstance("f1", new Vector2Int(10, 10))
            {
                State = FieldState.Planted,
                CurrentCropId = "wheat",
                PlantedUtcTicks = startTicks
            };

            long midTicks = startTicks + System.TimeSpan.FromSeconds(5).Ticks;
            Assert.AreEqual(0.5f, field.GetGrowthProgress(midTicks, wheat), 0.01f);
            Assert.AreEqual(3, field.GetGrowthStage(midTicks, wheat));

            long endTicks = startTicks + System.TimeSpan.FromSeconds(11).Ticks;
            field.CheckAndUpdateState(endTicks, wheat);
            Assert.AreEqual(FieldState.Ready, field.State);
            Assert.AreEqual(1.0f, field.GetGrowthProgress(endTicks, wheat));
            Assert.AreEqual(5, field.GetGrowthStage(endTicks, wheat));
        }

        [Test]
        public void FarmManager_HarvestCrop_AwardsItemsAndXP_AndResetsField()
        {
            var timeService = new StandardGameTimeService();
            var farmManager = new FarmManager(_inventoryManager, _profile, timeService);

            var field = new FieldInstance("f1", new Vector2Int(10, 10))
            {
                State = FieldState.Ready,
                CurrentCropId = "wheat"
            };
            farmManager.RegisterField(field);

            int initialXp = _profile.CurrentXP;
            var result = farmManager.HarvestCrop("f1");

            Assert.AreEqual(FarmingOperationResult.Success, result);
            Assert.AreEqual(FieldState.Empty, field.State);
            Assert.AreEqual(2, _inventoryManager.GetQuantity("crop_wheat"));
            Assert.Greater(_profile.CurrentXP, initialXp);
        }

        [Test]
        public void FarmManager_HarvestCrop_FullStorageRejection()
        {
            var tinyInventory = new InventoryManager(1);
            var timeService = new StandardGameTimeService();
            var farmManager = new FarmManager(tinyInventory, _profile, timeService);

            tinyInventory.AddItem("stone", "Stone", ItemType.RawMaterial, 1);

            var field = new FieldInstance("f1", new Vector2Int(10, 10))
            {
                State = FieldState.Ready,
                CurrentCropId = "wheat"
            };
            farmManager.RegisterField(field);

            var result = farmManager.HarvestCrop("f1");
            Assert.AreEqual(FarmingOperationResult.StorageFull, result);
            Assert.AreEqual(FieldState.Ready, field.State);
        }
        #endregion

        #region Buildings & Construction System Tests
        [Test]
        public void BuildingLibrary_LoadsConfigurationsCorrectly()
        {
            var house = BuildingLibrary.GetBuilding("small_house");
            Assert.IsNotNull(house);
            Assert.AreEqual("Small House", house.Name);
            Assert.AreEqual(2, house.Width);
            Assert.AreEqual(2, house.Height);
            Assert.AreEqual(2, house.PopulationCapacity);

            var workshop = BuildingLibrary.GetBuilding("small_workshop");
            Assert.IsNotNull(workshop);
            Assert.AreEqual(2, workshop.RequiredMaterials.Count);

            var market = BuildingLibrary.GetBuilding("town_market");
            Assert.IsNotNull(market);
            Assert.AreEqual("Town Market", market.Name);
        }

        [Test]
        public void BuildingManager_StartConstruction_DeductsResourcesAndStartsTimer()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);

            long initialCoins = _profile.Coins;
            var result = buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var instance);

            Assert.AreEqual(BuildingOperationResult.Success, result);
            Assert.IsNotNull(instance);
            Assert.AreEqual(BuildingState.UnderConstruction, instance.State);
            Assert.AreEqual(initialCoins - 100, _profile.Coins);
        }

        [Test]
        public void BuildingManager_StartConstruction_RejectsInsufficientCoins()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);

            _profile.Coins = 10;
            var result = buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var instance);

            Assert.AreEqual(BuildingOperationResult.CannotAffordCoins, result);
            Assert.IsNull(instance);
        }

        [Test]
        public void BuildingInstance_OfflineConstructionAndIdempotentCompletion()
        {
            long startTicks = System.DateTime.UtcNow.Ticks;
            var houseConfig = BuildingLibrary.GetBuilding("small_house");

            var instance = new BuildingInstance("bldg_1", "small_house", new Vector2Int(10, 10))
            {
                State = BuildingState.UnderConstruction,
                ConstructionStartUtcTicks = startTicks
            };

            long midTicks = startTicks + System.TimeSpan.FromSeconds(5).Ticks;
            Assert.AreEqual(0.33f, instance.GetConstructionProgress(midTicks, houseConfig), 0.05f);

            long endTicks = startTicks + System.TimeSpan.FromSeconds(16).Ticks;
            Assert.IsTrue(instance.CheckAndUpdateState(endTicks, houseConfig));
            Assert.AreEqual(BuildingState.Completed, instance.State);
            Assert.AreEqual(1.0f, instance.GetConstructionProgress(endTicks, houseConfig));
        }

        [Test]
        public void BuildingManager_BarnConstruction_ExpandsInventoryStorageCapacity()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);

            Assert.AreEqual(50, _inventoryManager.MaxCapacity);

            buildingManager.StartConstruction("barn", new Vector2Int(10, 10), RotationAngle.Deg0, out var instance);
            buildingManager.DevInstantCompleteConstruction(instance.InstanceId);

            Assert.AreEqual(120, _inventoryManager.MaxCapacity);
        }

        [Test]
        public void BuildingManager_BuildingUpgrades_IncreasesLevelAndPopulationCapacity()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);

            buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var instance);
            buildingManager.DevInstantCompleteConstruction(instance.InstanceId);

            Assert.AreEqual(2, buildingManager.TotalPopulationCapacity);

            var result = buildingManager.StartUpgrade(instance.InstanceId);
            Assert.AreEqual(BuildingOperationResult.Success, result);
            buildingManager.DevInstantCompleteConstruction(instance.InstanceId);

            Assert.AreEqual(2, instance.Level);
            Assert.AreEqual(7, buildingManager.TotalPopulationCapacity);
        }

        [Test]
        public void BuildingManager_ConstructionWithAdventureResources()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);

            _profile.Level = 5;
            _profile.Coins = 1000;

            var resultNoMat = buildingManager.StartConstruction("small_workshop", new Vector2Int(10, 10), RotationAngle.Deg0, out _);
            Assert.AreEqual(BuildingOperationResult.MissingMaterials, resultNoMat);

            _inventoryManager.AddItem("item_wood", "Wood", ItemType.RawMaterial, 10);
            _inventoryManager.AddItem("item_stone", "Stone", ItemType.RawMaterial, 5);

            var resultSuccess = buildingManager.StartConstruction("small_workshop", new Vector2Int(10, 10), RotationAngle.Deg0, out var bldg);
            Assert.AreEqual(BuildingOperationResult.Success, resultSuccess);
            Assert.AreEqual(0, _inventoryManager.GetQuantity("item_wood"));
            Assert.AreEqual(0, _inventoryManager.GetQuantity("item_stone"));
        }
        #endregion

        #region Production & Factory System Tests
        [Test]
        public void RecipeLibrary_LoadsConfigurationsCorrectly()
        {
            var feedRecipe = RecipeLibrary.GetRecipe("recipe_animal_feed");
            Assert.IsNotNull(feedRecipe);
            Assert.AreEqual("Animal Feed", feedRecipe.Name);
            Assert.AreEqual("feed_mill", feedRecipe.BuildingId);
            Assert.AreEqual("item_animal_feed", feedRecipe.OutputItemId);
            Assert.AreEqual(1, feedRecipe.Ingredients.Count);

            var brickRecipe = RecipeLibrary.GetRecipe("recipe_brick");
            Assert.IsNotNull(brickRecipe);
            Assert.AreEqual("item_brick", brickRecipe.OutputItemId);
            Assert.AreEqual("item_clay", brickRecipe.Ingredients[0].ItemId);
        }

        [Test]
        public void ProductionManager_StartProductionJob_DeductsIngredientsOnStart()
        {
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(_inventoryManager, _profile, timeService);

            var feedMill = new ProductionBuildingInstance("pm_1", "feed_mill", 2);
            prodManager.RegisterProductionBuilding(feedMill);

            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 5);

            var result = prodManager.StartProductionJob("pm_1", "recipe_animal_feed");
            Assert.AreEqual(ProductionOperationResult.Success, result);
            Assert.AreEqual(1, feedMill.JobsQueue.Count);
            Assert.AreEqual(ProductionJobState.Producing, feedMill.JobsQueue[0].State);
            Assert.AreEqual(3, _inventoryManager.GetQuantity("crop_wheat"));
        }

        [Test]
        public void ProductionManager_BrickProductionWithClayResource()
        {
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(_inventoryManager, _profile, timeService);
            _profile.Level = 5;

            var bakery = new ProductionBuildingInstance("bakery_1", "bakery", 2);
            prodManager.RegisterProductionBuilding(bakery);

            _inventoryManager.AddItem("item_clay", "Clay", ItemType.RawMaterial, 3);

            var result = prodManager.StartProductionJob("bakery_1", "recipe_brick");
            Assert.AreEqual(ProductionOperationResult.Success, result);
            Assert.AreEqual(0, _inventoryManager.GetQuantity("item_clay"));

            prodManager.DevInstantCompleteCurrentJob("bakery_1");
            prodManager.CollectProduct("bakery_1");

            Assert.AreEqual(1, _inventoryManager.GetQuantity("item_brick"));
        }
        #endregion

        #region Orders & Delivery System Tests
        [Test]
        public void OrderGenerator_GeneratesLevelAppropriateOrders()
        {
            long now = System.DateTime.UtcNow.Ticks;
            var orderLvl1 = OrderGenerator.GenerateOrderForLevel(1, now);

            Assert.IsNotNull(orderLvl1);
            Assert.GreaterOrEqual(orderLvl1.Requirements.Count, 1);
            Assert.Greater(orderLvl1.Reward.Coins, 0);
            Assert.Greater(orderLvl1.Reward.Xp, 0);
        }

        [Test]
        public void OrderManager_AtomicFulfillOrder_DeductsItemsAndGrantsRewards()
        {
            var timeService = new StandardGameTimeService();
            var orderManager = new OrderManager(_inventoryManager, _economyManager, _profile, timeService, maxSlots: 3);

            _inventoryManager.AddItem("item_bread", "Bread", ItemType.ManufacturedGood, 5);

            var breadOrder = new OrderInstance(
                "o_test_1",
                "cust_emma",
                OrderType.Customer,
                new List<OrderRequirement> { new OrderRequirement("item_bread", 2) },
                new OrderReward(100, 20),
                timeService.CurrentUtcTicks
            );

            orderManager.LoadActiveOrders(new List<OrderInstance> { breadOrder });

            long initialCoins = _profile.Coins;
            int initialXp = _profile.CurrentXP;

            var result = orderManager.FulfillOrder("o_test_1");

            Assert.AreEqual(OrderOperationResult.Success, result);
            Assert.AreEqual(3, _inventoryManager.GetQuantity("item_bread"));
            Assert.AreEqual(initialCoins + 100, _profile.Coins);
            Assert.AreEqual(initialXp + 20, _profile.CurrentXP);
        }
        #endregion

        #region Population, Residents & Happiness System Tests
        [Test]
        public void PopulationManager_HousingCapacityFromCompletedHousesOnly()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);
            var popManager = new PopulationManager(buildingManager, _profile, timeService);

            buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var h1);
            popManager.RecalculatePopulationAndAssignments();

            var stats1 = popManager.GetPopulationStats();
            Assert.AreEqual(0, stats1.TotalHousingCapacity);

            buildingManager.DevInstantCompleteConstruction(h1.InstanceId);
            var stats2 = popManager.GetPopulationStats();

            Assert.AreEqual(2, stats2.TotalHousingCapacity);
            Assert.AreEqual(2, stats2.CurrentPopulation);
        }
        #endregion

        #region Task 9 Market & Trading Economy Tests
        [Test]
        public void MarketManager_BuyItem_AtomicTransactionAndStockReduction()
        {
            var timeService = new StandardGameTimeService();
            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            _profile.Coins = 500;
            long initialCoins = _profile.Coins;

            var buyResult = marketManager.BuyItem("crop_wheat", 5);

            Assert.AreEqual(MarketOperationResult.Success, buyResult);
            Assert.AreEqual(initialCoins - 50, _profile.Coins);
            Assert.AreEqual(5, _inventoryManager.GetQuantity("crop_wheat"));

            var listing = marketManager.GetListing("crop_wheat");
            Assert.AreEqual(95, listing.CurrentStock);

            var history = marketManager.ExportHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(TransactionType.Buy, history[0].Type);
            Assert.AreEqual(50, history[0].TotalPrice);
        }

        [Test]
        public void MarketManager_BuyItem_RejectsInsufficientCoinsOrStorageOrLockedLevel()
        {
            var timeService = new StandardGameTimeService();
            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            _profile.Coins = 10;
            Assert.AreEqual(MarketOperationResult.InsufficientCoins, marketManager.BuyItem("crop_wheat", 5));
            Assert.AreEqual(10, _profile.Coins);

            _profile.Coins = 1000;
            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 50);
            Assert.AreEqual(MarketOperationResult.InsufficientStorage, marketManager.BuyItem("crop_wheat", 1));

            _profile.Level = 1;
            Assert.AreEqual(MarketOperationResult.LevelLocked, marketManager.BuyItem("crop_sugarcane", 1));
        }

        [Test]
        public void MarketManager_SellItem_AtomicEarningsAndInventoryDeduction()
        {
            var timeService = new StandardGameTimeService();
            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            _profile.Coins = 100;
            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 10);

            var sellResult = marketManager.SellItem("crop_wheat", 5);

            Assert.AreEqual(MarketOperationResult.Success, sellResult);
            Assert.AreEqual(130, _profile.Coins);
            Assert.AreEqual(5, _inventoryManager.GetQuantity("crop_wheat"));

            var history = marketManager.ExportHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(TransactionType.Sell, history[0].Type);
            Assert.AreEqual(30, history[0].TotalPrice);
        }

        [Test]
        public void MarketManager_BuyAndSellPriceDifference_PreventsInfiniteCurrencyLoop()
        {
            var timeService = new StandardGameTimeService();
            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            _profile.Coins = 100;

            marketManager.BuyItem("crop_wheat", 1);
            Assert.AreEqual(90, _profile.Coins);

            marketManager.SellItem("crop_wheat", 1);
            Assert.AreEqual(96, _profile.Coins);

            Assert.Less(_profile.Coins, 100);
        }

        [Test]
        public void MarketManager_UTCUtOfflineRestocking()
        {
            var timeService = new StandardGameTimeService();
            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            marketManager.DevEmptyStock();
            Assert.AreEqual(0, marketManager.GetListing("crop_wheat").CurrentStock);

            timeService.AdvanceTime(System.TimeSpan.FromSeconds(1800));
            marketManager.RecalculateRestocks();

            Assert.AreEqual(20, marketManager.GetListing("crop_wheat").CurrentStock);

            timeService.AdvanceTime(System.TimeSpan.FromHours(10));
            marketManager.RecalculateRestocks();

            Assert.AreEqual(100, marketManager.GetListing("crop_wheat").CurrentStock);
        }
        #endregion

        #region Task 10 Quest, Goals & Progression System Tests
        [Test]
        public void QuestManager_MainQuestChainAndPrerequisites()
        {
            var timeService = new StandardGameTimeService();
            var questManager = new QuestManager(_profile, _economyManager, _inventoryManager, timeService);

            var activeQuests = questManager.GetActiveQuests();
            Assert.AreEqual(1, activeQuests.FindAll(q => QuestLibrary.GetQuest(q.QuestId)?.Type == QuestType.Main).Count);

            // First Main Quest is "main_q1_start_town"
            var q1 = activeQuests.Find(q => q.QuestId == "main_q1_start_town");
            Assert.IsNotNull(q1);
            Assert.AreEqual(QuestState.Active, q1.State);

            // Trigger objective: Build 1 Small House
            questManager.OnGameplayEvent(ObjectiveType.Build, "small_house", 1);
            Assert.AreEqual(QuestState.Completed, q1.State);

            // Atomic Reward Claim
            long coinsBefore = _profile.Coins;
            var claimResult = questManager.ClaimReward("main_q1_start_town");

            Assert.AreEqual(QuestClaimResult.Success, claimResult);
            Assert.AreEqual(coinsBefore + 100, _profile.Coins);

            // Second Main Quest ("main_q2_grow_crop") unlocks dynamically
            var q2 = questManager.GetActiveQuests().Find(q => q.QuestId == "main_q2_grow_crop");
            Assert.IsNotNull(q2);
            Assert.AreEqual(QuestState.Active, q2.State);
        }

        [Test]
        public void QuestManager_EventDrivenObjectiveProgressAndRewardClaiming()
        {
            var timeService = new StandardGameTimeService();
            var questManager = new QuestManager(_profile, _economyManager, _inventoryManager, timeService);

            var sideQ1 = questManager.GetActiveQuests().Find(q => q.QuestId == "side_q1_farmer_request");
            Assert.IsNotNull(sideQ1);

            // Harvest wheat incremental events
            questManager.OnGameplayEvent(ObjectiveType.Harvest, "wheat", 4);
            Assert.AreEqual(4, sideQ1.ObjectivesProgress[0].CurrentAmount);
            Assert.AreEqual(QuestState.Active, sideQ1.State);

            questManager.OnGameplayEvent(ObjectiveType.Harvest, "wheat", 6);
            Assert.AreEqual(10, sideQ1.ObjectivesProgress[0].CurrentAmount);
            Assert.AreEqual(QuestState.Completed, sideQ1.State);

            // Claim Reward
            Assert.AreEqual(QuestClaimResult.Success, questManager.ClaimReward("side_q1_farmer_request"));

            // Double Claim Prevention
            Assert.AreEqual(QuestClaimResult.AlreadyClaimed, questManager.ClaimReward("side_q1_farmer_request"));
        }

        [Test]
        public void QuestManager_UTCDailyResetHandling()
        {
            var timeService = new StandardGameTimeService();
            var questManager = new QuestManager(_profile, _economyManager, _inventoryManager, timeService);

            questManager.OnGameplayEvent(ObjectiveType.Harvest, "wheat", 5);
            var daily1 = questManager.GetActiveQuests().Find(q => q.QuestId == "daily_g1_harvest_crops");
            Assert.AreEqual(QuestState.Completed, daily1.State);

            questManager.ClaimReward("daily_g1_harvest_crops");
            Assert.IsTrue(questManager.GetClaimedQuestIds().Contains("daily_g1_harvest_crops"));

            // Advance 25 hours -> Daily Goals Reset
            timeService.AdvanceTime(System.TimeSpan.FromHours(25));
            questManager.CheckDailyReset();

            Assert.IsFalse(questManager.GetClaimedQuestIds().Contains("daily_g1_harvest_crops"));
            var resetDaily1 = questManager.GetActiveQuests().Find(q => q.QuestId == "daily_g1_harvest_crops");
            Assert.IsNotNull(resetDaily1);
            Assert.AreEqual(QuestState.Active, resetDaily1.State);
            Assert.AreEqual(0, resetDaily1.ObjectivesProgress[0].CurrentAmount);
        }
        #endregion

        [Test]
        public void SaveSystem_SaveAndLoad_Version12Schema()
        {
            var storage = new MockStorage();
            var saveSystem = new LocalSaveSystem("save_v12.json", storage);

            var initialSave = new SaveData
            {
                Version = 12,
                PlayerProfile = new PlayerProfile { Level = 12, Coins = 5000, Gems = 100 },
                ActiveQuests = new List<QuestInstance>
                {
                    new QuestInstance("main_q1_start_town", QuestState.Active, new List<QuestObjectiveProgress> { new QuestObjectiveProgress("obj_build_house", 1) }, System.DateTime.UtcNow.Ticks)
                },
                ClaimedQuestIds = new List<string> { "ms_first_harvest" },
                LastDailyResetUtcTicks = System.DateTime.UtcNow.Ticks
            };

            saveSystem.Save(initialSave);
            Assert.IsTrue(storage.Exists("save_v12.json"));

            var loadedSave = saveSystem.Load();
            Assert.AreEqual(12, loadedSave.Version);
            Assert.AreEqual(1, loadedSave.ActiveQuests.Count);
            Assert.AreEqual("main_q1_start_town", loadedSave.ActiveQuests[0].QuestId);
            Assert.AreEqual(1, loadedSave.ClaimedQuestIds.Count);
            Assert.AreEqual("ms_first_harvest", loadedSave.ClaimedQuestIds[0]);
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
