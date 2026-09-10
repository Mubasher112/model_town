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

            Assert.IsTrue(expansionManager.UnlockZone("zone_north"));
            Assert.IsFalse(grid.GetTile(northZoneCoord).IsLocked);
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

            var barn = BuildingLibrary.GetBuilding("barn");
            Assert.IsNotNull(barn);
            Assert.AreEqual(20, barn.StorageCapacityBonus);
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
            Assert.AreEqual("crop_wheat", feedRecipe.Ingredients[0].ItemId);
            Assert.AreEqual(2, feedRecipe.Ingredients[0].Quantity);

            var bakeryRecipes = RecipeLibrary.GetRecipesForBuilding("bakery");
            Assert.AreEqual(2, bakeryRecipes.Count);
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
        public void ProductionManager_StartProductionJob_RejectsMissingIngredientsAndFullQueue()
        {
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(_inventoryManager, _profile, timeService);

            var feedMill = new ProductionBuildingInstance("pm_1", "feed_mill", 1);
            prodManager.RegisterProductionBuilding(feedMill);

            var noIngResult = prodManager.StartProductionJob("pm_1", "recipe_animal_feed");
            Assert.AreEqual(ProductionOperationResult.MissingIngredients, noIngResult);

            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 10);

            Assert.AreEqual(ProductionOperationResult.Success, prodManager.StartProductionJob("pm_1", "recipe_animal_feed"));
            Assert.AreEqual(ProductionOperationResult.QueueFull, prodManager.StartProductionJob("pm_1", "recipe_animal_feed"));
        }

        [Test]
        public void ProductionJob_OfflineProgressAndCollection()
        {
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(_inventoryManager, _profile, timeService);

            var feedMill = new ProductionBuildingInstance("pm_1", "feed_mill", 2);
            prodManager.RegisterProductionBuilding(feedMill);

            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 10);
            prodManager.StartProductionJob("pm_1", "recipe_animal_feed");

            prodManager.DevInstantCompleteCurrentJob("pm_1");
            Assert.AreEqual(ProductionJobState.Ready, feedMill.JobsQueue[0].State);

            int xpBefore = _profile.CurrentXP;
            var collectResult = prodManager.CollectProduct("pm_1");

            Assert.AreEqual(ProductionOperationResult.Success, collectResult);
            Assert.AreEqual(0, feedMill.JobsQueue.Count);
            Assert.AreEqual(1, _inventoryManager.GetQuantity("item_animal_feed"));
            Assert.Greater(_profile.CurrentXP, xpBefore);
        }

        [Test]
        public void ProductionManager_ProductionChain_WheatToFlourToBread()
        {
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(_inventoryManager, _profile, timeService);
            _profile.Level = 5;

            var bakery = new ProductionBuildingInstance("bakery_1", "bakery", 2);
            prodManager.RegisterProductionBuilding(bakery);

            _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 4);
            _inventoryManager.AddItem("item_sugar", "Sugar", ItemType.ManufacturedGood, 2);

            prodManager.StartProductionJob("bakery_1", "recipe_flour");
            prodManager.DevInstantCompleteCurrentJob("bakery_1");
            prodManager.CollectProduct("bakery_1");

            Assert.AreEqual(1, _inventoryManager.GetQuantity("item_flour"));

            prodManager.StartProductionJob("bakery_1", "recipe_bread");
            prodManager.DevInstantCompleteCurrentJob("bakery_1");
            prodManager.CollectProduct("bakery_1");

            Assert.AreEqual(1, _inventoryManager.GetQuantity("item_bread"));
        }

        [Test]
        public void ProductionManager_CollectProduct_FullStorageRejection()
        {
            var tinyInventory = new InventoryManager(1);
            var timeService = new StandardGameTimeService();
            var prodManager = new ProductionManager(tinyInventory, _profile, timeService);

            var feedMill = new ProductionBuildingInstance("pm_1", "feed_mill", 2);
            prodManager.RegisterProductionBuilding(feedMill);

            tinyInventory.AddItem("crop_wheat", "Wheat", ItemType.Crop, 1);

            var headJob = new ProductionJob("j1", "recipe_animal_feed") { State = ProductionJobState.Ready };
            feedMill.JobsQueue.Add(headJob);

            var result = prodManager.CollectProduct("pm_1");
            Assert.AreEqual(ProductionOperationResult.StorageFull, result);
            Assert.AreEqual(1, feedMill.JobsQueue.Count);
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
            Assert.AreEqual(1, orderManager.GetOrderHistory().Count);
        }

        [Test]
        public void OrderManager_AtomicValidationFailure_LeavesInventoryUntouched()
        {
            var timeService = new StandardGameTimeService();
            var orderManager = new OrderManager(_inventoryManager, _economyManager, _profile, timeService, maxSlots: 3);

            _inventoryManager.AddItem("item_bread", "Bread", ItemType.ManufacturedGood, 1);

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

            Assert.AreEqual(OrderOperationResult.MissingRequirements, result);
            Assert.AreEqual(1, _inventoryManager.GetQuantity("item_bread"));
            Assert.AreEqual(initialCoins, _profile.Coins);
            Assert.AreEqual(initialXp, _profile.CurrentXP);
        }

        [Test]
        public void OrderManager_OrderExpiration_RemovesExpiredOrders()
        {
            var timeService = new StandardGameTimeService();
            var orderManager = new OrderManager(_inventoryManager, _economyManager, _profile, timeService, maxSlots: 3);

            long startTicks = System.DateTime.UtcNow.Ticks;
            var expiringOrder = new OrderInstance(
                "o_exp",
                "cust_emma",
                OrderType.Customer,
                new List<OrderRequirement> { new OrderRequirement("crop_wheat", 1) },
                new OrderReward(10, 5),
                startTicks,
                expirationDurationSeconds: 10
            );

            orderManager.LoadActiveOrders(new List<OrderInstance> { expiringOrder });

            Assert.IsFalse(expiringOrder.IsExpired(startTicks + System.TimeSpan.FromSeconds(5).Ticks));
            Assert.IsTrue(expiringOrder.IsExpired(startTicks + System.TimeSpan.FromSeconds(11).Ticks));
        }

        [Test]
        public void OrderManager_EnsuresMinimumActiveOrderSlots()
        {
            var timeService = new StandardGameTimeService();
            var orderManager = new OrderManager(_inventoryManager, _economyManager, _profile, timeService, maxSlots: 3);

            orderManager.EnsureMinimumOrders();
            Assert.AreEqual(3, orderManager.GetActiveOrders().Count);
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

            // House 1: Under construction -> 0 capacity contribution
            buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var h1);
            popManager.RecalculatePopulationAndAssignments();

            var stats1 = popManager.GetPopulationStats();
            Assert.AreEqual(0, stats1.TotalHousingCapacity);
            Assert.AreEqual(0, stats1.CurrentPopulation);

            // House 1 completed -> Capacity 2, auto-spawns 2 residents
            buildingManager.DevInstantCompleteConstruction(h1.InstanceId);
            var stats2 = popManager.GetPopulationStats();

            Assert.AreEqual(2, stats2.TotalHousingCapacity);
            Assert.AreEqual(2, stats2.CurrentPopulation);
        }

        [Test]
        public void PopulationManager_HouseRemoval_SafelyUnassignsAndReassignsResidents()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);
            var popManager = new PopulationManager(buildingManager, _profile, timeService);

            // Build House 1 (Cap 2) and House 2 (Cap 2)
            buildingManager.StartConstruction("small_house", new Vector2Int(10, 10), RotationAngle.Deg0, out var h1);
            buildingManager.DevInstantCompleteConstruction(h1.InstanceId);
            buildingManager.StartConstruction("small_house", new Vector2Int(15, 10), RotationAngle.Deg0, out var h2);
            buildingManager.DevInstantCompleteConstruction(h2.InstanceId);

            var stats1 = popManager.GetPopulationStats();
            Assert.AreEqual(4, stats1.TotalHousingCapacity);
            Assert.AreEqual(4, stats1.CurrentPopulation);

            // Remove House 1 -> Capacity becomes 2. 2 residents stay housed, 2 become unassigned
            buildingManager.RemoveBuilding(h1.InstanceId);
            var stats2 = popManager.GetPopulationStats();

            Assert.AreEqual(2, stats2.TotalHousingCapacity);
            Assert.AreEqual(4, stats2.CurrentPopulation); // Residents NOT deleted!
            Assert.AreEqual(2, stats2.UnassignedResidentsCount);
        }

        [Test]
        public void HappinessManager_CalculatesModifiersAndClampsScore()
        {
            var grid = new WorldGrid(30, 30);
            new LandExpansionManager(grid).UnlockAllZonesDev();
            var placementManager = new ObjectPlacementManager(grid);
            var timeService = new StandardGameTimeService();
            var buildingManager = new BuildingManager(placementManager, _economyManager, _inventoryManager, _profile, timeService);
            var happinessManager = new HappinessManager();

            // Build Town Hall (+10 happiness bonus)
            buildingManager.StartConstruction("town_hall", new Vector2Int(10, 10), RotationAngle.Deg0, out var th);
            buildingManager.DevInstantCompleteConstruction(th.InstanceId);

            happinessManager.RecalculateHappiness(buildingManager.GetAllBuildings(), currentPopulation: 4, totalHousingCapacity: 4);

            // Base 50 + 10 = 60 ("Good")
            Assert.AreEqual(60, happinessManager.CurrentHappinessScore);
            Assert.AreEqual("Good", happinessManager.HappinessRating);

            // Add Housing Shortage penalty (6 pop, 2 housing capacity -> 4 unhoused = -40 penalty)
            happinessManager.RecalculateHappiness(buildingManager.GetAllBuildings(), currentPopulation: 6, totalHousingCapacity: 2);

            // Base 50 + 10 - 40 = 20 ("Low")
            Assert.AreEqual(20, happinessManager.CurrentHappinessScore);
            Assert.AreEqual("Low", happinessManager.HappinessRating);
        }
        #endregion

        [Test]
        public void SaveSystem_SaveAndLoad_WorldPersistence()
        {
            var storage = new MockStorage();
            var saveSystem = new LocalSaveSystem("save_v7.json", storage);

            var initialSave = new SaveData
            {
                Version = 7,
                PlayerProfile = new PlayerProfile { Level = 5, Coins = 1200, Gems = 50 },
                UnlockedZoneIds = new List<string> { "zone_start", "zone_north" },
                RoadTiles = new List<SavedRoadTile> { new SavedRoadTile(10, 10), new SavedRoadTile(10, 11) },
                PlacedObjects = new List<SavedPlacedObject>
                {
                    new SavedPlacedObject { ObjectId = "bldg_1", ObjectTypeId = "house", X = 12, Y = 12, BaseWidth = 2, BaseHeight = 2, RotationDegrees = 90 }
                },
                Fields = new List<SavedField>
                {
                    new SavedField { FieldId = "f1", X = 10, Y = 10, Width = 1, Height = 1, State = (int)FieldState.Growing, CurrentCropId = "wheat", PlantedUtcTicks = System.DateTime.UtcNow.Ticks }
                },
                Buildings = new List<SavedBuilding>
                {
                    new SavedBuilding { InstanceId = "b1", BuildingId = "small_house", X = 15, Y = 15, BaseWidth = 2, BaseHeight = 2, RotationDegrees = 0, Level = 1, State = (int)BuildingState.Completed }
                },
                ProductionBuildings = new List<SavedProductionBuilding>
                {
                    new SavedProductionBuilding
                    {
                        BuildingInstanceId = "pb1",
                        BuildingId = "feed_mill",
                        QueueCapacity = 2,
                        JobsQueue = new List<SavedProductionJob>
                        {
                            new SavedProductionJob { JobId = "j1", RecipeId = "recipe_animal_feed", State = (int)ProductionJobState.Producing, StartUtcTicks = System.DateTime.UtcNow.Ticks }
                        }
                    }
                },
                ActiveOrders = new List<SavedOrder>
                {
                    new SavedOrder { OrderId = "o1", CustomerId = "cust_emma", Type = (int)OrderType.Customer, RewardCoins = 100, RewardXp = 20, State = (int)OrderState.Active, CreationUtcTicks = System.DateTime.UtcNow.Ticks }
                },
                OrderHistory = new List<SavedOrderHistory>
                {
                    new SavedOrderHistory { OrderId = "o0", CustomerId = "cust_john", CoinsEarned = 50, XpEarned = 10, CompletionUtcTicks = System.DateTime.UtcNow.Ticks }
                },
                Residents = new List<SavedResident>
                {
                    new SavedResident { ResidentId = "r1", ResidentTypeId = "res_farmer", DisplayName = "Emma", AssignedHouseInstanceId = "b1", State = (int)ResidentState.AtHome, CreationUtcTicks = System.DateTime.UtcNow.Ticks }
                }
            };

            saveSystem.Save(initialSave);
            Assert.IsTrue(storage.Exists("save_v7.json"));

            var loadedSave = saveSystem.Load();
            Assert.AreEqual(7, loadedSave.Version);
            Assert.AreEqual(5, loadedSave.PlayerProfile.Level);
            Assert.AreEqual(2, loadedSave.UnlockedZoneIds.Count);
            Assert.AreEqual(2, loadedSave.RoadTiles.Count);
            Assert.AreEqual(1, loadedSave.PlacedObjects.Count);
            Assert.AreEqual(1, loadedSave.Fields.Count);
            Assert.AreEqual(1, loadedSave.Buildings.Count);
            Assert.AreEqual(1, loadedSave.ProductionBuildings.Count);
            Assert.AreEqual(1, loadedSave.ActiveOrders.Count);
            Assert.AreEqual(1, loadedSave.OrderHistory.Count);
            Assert.AreEqual(1, loadedSave.Residents.Count);
            Assert.AreEqual("r1", loadedSave.Residents[0].ResidentId);
            Assert.AreEqual("b1", loadedSave.Residents[0].AssignedHouseInstanceId);
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
