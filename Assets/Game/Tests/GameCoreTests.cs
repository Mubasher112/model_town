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

            // Buy 5 Wheat (10 coins each = 50 coins)
            var buyResult = marketManager.BuyItem("crop_wheat", 5);

            Assert.AreEqual(MarketOperationResult.Success, buyResult);
            Assert.AreEqual(initialCoins - 50, _profile.Coins);
            Assert.AreEqual(5, _inventoryManager.GetQuantity("crop_wheat"));

            var listing = marketManager.GetListing("crop_wheat");
            Assert.AreEqual(95, listing.CurrentStock); // 100 - 5 = 95 stock remaining

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
            // Insufficient coins for 5 Wheat (50 coins)
            Assert.AreEqual(MarketOperationResult.InsufficientCoins, marketManager.BuyItem("crop_wheat", 5));
            Assert.AreEqual(10, _profile.Coins);

            _profile.Coins = 1000;
            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 50); // Storage Full (50/50)
            Assert.AreEqual(MarketOperationResult.InsufficientStorage, marketManager.BuyItem("crop_wheat", 1));

            // Rejects level-locked item (Sugarcane requires Level 5)
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

            // Sell 5 Wheat (6 coins sell price each = 30 coins earned)
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

            // Buy 1 Wheat (10 coins) -> Balance 90
            marketManager.BuyItem("crop_wheat", 1);
            Assert.AreEqual(90, _profile.Coins);

            // Immediately sell 1 Wheat (6 coins) -> Balance 96
            marketManager.SellItem("crop_wheat", 1);
            Assert.AreEqual(96, _profile.Coins);

            // Net loss of 4 coins per cycle prevents currency duplication exploits
            Assert.Less(_profile.Coins, 100);
        }

        [Test]
        public void MarketManager_UTCUtOfflineRestocking()
        {
            var timeService = new StandardGameTimeService();
            long startTicks = timeService.CurrentUtcTicks;

            var marketManager = new MarketManager(_economyManager, _inventoryManager, _profile, timeService);

            // Deplete Wheat stock to 0
            marketManager.DevEmptyStock();
            Assert.AreEqual(0, marketManager.GetListing("crop_wheat").CurrentStock);

            // Advance 30 minutes (1800s) -> +20 Restock Amount
            timeService.AdvanceTime(System.TimeSpan.FromSeconds(1800));
            marketManager.RecalculateRestocks();

            Assert.AreEqual(20, marketManager.GetListing("crop_wheat").CurrentStock);

            // Advance 10 hours -> Clamped to Max Stock (100)
            timeService.AdvanceTime(System.TimeSpan.FromHours(10));
            marketManager.RecalculateRestocks();

            Assert.AreEqual(100, marketManager.GetListing("crop_wheat").CurrentStock);
        }
        #endregion

        #region Task 10 Social, Friends & Profiles System Tests
        [Test]
        public void MockSocialService_ProfileEditingAndValidation()
        {
            var mockSocial = new MockSocialService();
            bool updateSuccess = false;

            mockSocial.UpdateProfile("   ", "avatar_farmer", (s, err) => updateSuccess = s);
            Assert.IsFalse(updateSuccess);

            mockSocial.UpdateProfile("Green Valley", "avatar_farmer", (s, err) => updateSuccess = s);
            Assert.IsTrue(updateSuccess);

            mockSocial.GetMyProfile((s, profile, err) =>
            {
                Assert.IsTrue(s);
                Assert.AreEqual("Green Valley", profile.DisplayName);
                Assert.AreEqual("avatar_farmer", profile.AvatarId);
            });
        }

        [Test]
        public void MockSocialService_FriendRequestFlow_SendAcceptRejectRemove()
        {
            var mockSocial = new MockSocialService();

            bool selfReqSuccess = true;
            mockSocial.SendFriendRequest("p_my_id", (s, err) => selfReqSuccess = s);
            Assert.IsFalse(selfReqSuccess);

            bool sendSuccess = false;
            mockSocial.SendFriendRequest("p_sunny", (s, err) => sendSuccess = s);
            Assert.IsTrue(sendSuccess);

            bool dupReqSuccess = true;
            mockSocial.SendFriendRequest("p_sunny", (s, err) => dupReqSuccess = s);
            Assert.IsFalse(dupReqSuccess);

            bool acceptSuccess = false;
            mockSocial.AcceptFriendRequest("p_sunny", (s, err) => acceptSuccess = s);
            Assert.IsTrue(acceptSuccess);

            List<FriendRelationship> friends = null;
            mockSocial.GetFriendsList((s, list, err) => friends = list);
            Assert.IsNotNull(friends);
            Assert.AreEqual(1, friends.Count);
            Assert.AreEqual("p_sunny", friends[0].TargetPlayerId);
            Assert.AreEqual(FriendStatus.Accepted, friends[0].Status);

            bool removeSuccess = false;
            mockSocial.RemoveFriend("p_sunny", (s, err) => removeSuccess = s);
            Assert.IsTrue(removeSuccess);

            mockSocial.GetFriendsList((s, list, err) => friends = list);
            Assert.AreEqual(0, friends.Count);
        }

        [Test]
        public void MockSocialService_PlayerSearchByDisplayName()
        {
            var mockSocial = new MockSocialService();
            List<SocialProfile> searchResults = null;

            mockSocial.SearchPlayers("Sunny", (s, list, err) => searchResults = list);
            Assert.IsNotNull(searchResults);
            Assert.AreEqual(1, searchResults.Count);
            Assert.AreEqual("p_sunny", searchResults[0].PlayerId);
        }

        [Test]
        public void SocialManager_ReadonlyVisitMode_EnforcesState()
        {
            var mockSocial = new MockSocialService();
            var socialManager = new SocialManager(mockSocial);

            Assert.AreEqual(GameTownMode.OwnTown, socialManager.CurrentTownMode);
            Assert.IsFalse(socialManager.IsVisitingFriend);

            bool visitSuccess = false;
            socialManager.StartVisitingFriend("p_sunny", (s, snap, err) => visitSuccess = s);

            Assert.IsTrue(visitSuccess);
            Assert.AreEqual(GameTownMode.FriendVisit, socialManager.CurrentTownMode);
            Assert.IsTrue(socialManager.IsVisitingFriend);
            Assert.IsNotNull(socialManager.VisitedTownSnapshot);
            Assert.AreEqual("Sunny Valley", socialManager.VisitedTownSnapshot.DisplayName);

            socialManager.ReturnToOwnTown();
            Assert.AreEqual(GameTownMode.OwnTown, socialManager.CurrentTownMode);
            Assert.IsFalse(socialManager.IsVisitingFriend);
        }

        [Test]
        public void SocialManager_AppreciateTownAndRestrictions()
        {
            var mockSocial = new MockSocialService();
            var socialManager = new SocialManager(mockSocial);

            Assert.IsFalse(socialManager.CanAppreciateTown("p_my_id"));

            bool appreciateSuccess = false;
            socialManager.AppreciateTown("p_sunny", (s, err) => appreciateSuccess = s);
            Assert.IsTrue(appreciateSuccess);

            Assert.IsFalse(socialManager.CanAppreciateTown("p_sunny"));
        }

        [Test]
        public void MockSocialService_BlockingPlayer_RestrictsInteractions()
        {
            var mockSocial = new MockSocialService();

            mockSocial.BlockPlayer("p_sunny", (s, err) => { });

            bool visitSuccess = true;
            mockSocial.VisitTown("p_sunny", (s, snap, err) => visitSuccess = s);
            Assert.IsFalse(visitSuccess);

            bool requestSuccess = true;
            mockSocial.SendFriendRequest("p_sunny", (s, err) => requestSuccess = s);
            Assert.IsFalse(requestSuccess);
        }

        [Test]
        public void MockSocialService_OfflineMode_GracefulFailure()
        {
            var mockSocial = new MockSocialService();
            mockSocial.SetOnline(false);

            bool requestSuccess = true;
            mockSocial.SendFriendRequest("p_sunny", (s, err) => requestSuccess = s);
            Assert.IsFalse(requestSuccess);

            bool searchSuccess = true;
            mockSocial.SearchPlayers("Sunny", (s, list, err) => searchSuccess = s);
            Assert.IsFalse(searchSuccess);
        }
        #endregion

        [Test]
        public void SaveSystem_SaveAndLoad_Version11Schema()
        {
            var storage = new MockStorage();
            var saveSystem = new LocalSaveSystem("save_v11.json", storage);

            var initialSave = new SaveData
            {
                Version = 11,
                PlayerProfile = new PlayerProfile { Level = 12, Coins = 5000, Gems = 100 },
                MarketListings = new List<MarketListing> { new MarketListing("crop_wheat", 80, System.DateTime.UtcNow.Ticks) },
                MarketHistory = new List<MarketTransaction>
                {
                    new MarketTransaction("tx_1", TransactionType.Buy, "crop_wheat", "Wheat", 5, 10, 50, System.DateTime.UtcNow.Ticks)
                }
            };

            saveSystem.Save(initialSave);
            Assert.IsTrue(storage.Exists("save_v11.json"));

            var loadedSave = saveSystem.Load();
            Assert.AreEqual(11, loadedSave.Version);
            Assert.AreEqual(1, loadedSave.MarketListings.Count);
            Assert.AreEqual(80, loadedSave.MarketListings[0].CurrentStock);
            Assert.AreEqual(1, loadedSave.MarketHistory.Count);
            Assert.AreEqual(TransactionType.Buy, loadedSave.MarketHistory[0].Type);
            Assert.AreEqual(50, loadedSave.MarketHistory[0].TotalPrice);
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
