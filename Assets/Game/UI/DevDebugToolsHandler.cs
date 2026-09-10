using UnityEngine;
using Game.World;
using Game.Farming;
using Game.Buildings;
using Game.Production;
using Game.Orders;
using Game.Residents;
using Game.Inventory;
using Game.Economy;
using Game.Save;
using Game.Adventure;
using Game.Social;
using Game.Services;

namespace Game.UI
{
    public class DevDebugToolsHandler : MonoBehaviour
    {
        private WorldGrid _grid;
        private ObjectPlacementManager _placementManager;
        private RoadManager _roadManager;
        private LandExpansionManager _expansionManager;
        private FarmManager _farmManager;
        private InventoryManager _inventoryManager;
        private BuildingManager _buildingManager;
        private EconomyManager _economyManager;
        private ProductionManager _productionManager;
        private OrderManager _orderManager;
        private PopulationManager _populationManager;
        private BuildingAccessibilityService _accessibilityService;
        private AdventureManager _adventureManager;
        private SocialManager _socialManager;
        private ISocialService _socialService;
        private LocalSaveSystem _saveSystem;

        private bool _showDevMenu = false;

        public void Initialize(
            WorldGrid grid,
            ObjectPlacementManager placementManager,
            RoadManager roadManager,
            LandExpansionManager expansionManager,
            LocalSaveSystem saveSystem,
            FarmManager farmManager = null,
            InventoryManager inventoryManager = null,
            BuildingManager buildingManager = null,
            EconomyManager economyManager = null,
            ProductionManager productionManager = null,
            OrderManager orderManager = null,
            PopulationManager populationManager = null,
            BuildingAccessibilityService accessibilityService = null,
            AdventureManager adventureManager = null,
            SocialManager socialManager = null,
            ISocialService socialService = null)
        {
            _grid = grid;
            _placementManager = placementManager;
            _roadManager = roadManager;
            _expansionManager = expansionManager;
            _saveSystem = saveSystem;
            _farmManager = farmManager;
            _inventoryManager = inventoryManager;
            _buildingManager = buildingManager;
            _economyManager = economyManager;
            _productionManager = productionManager;
            _orderManager = orderManager;
            _populationManager = populationManager;
            _accessibilityService = accessibilityService;
            _adventureManager = adventureManager;
            _socialManager = socialManager;
            _socialService = socialService;
        }

        public void DevSimulateSocialOffline() => _socialService?.SetOnline(false);
        public void DevSimulateSocialOnline() => _socialService?.SetOnline(true);
        public void DevSendMockFriendRequest() => _socialService?.SendFriendRequest("p_sunny", (s, err) => { });
        public void DevAcceptAllFriendRequests()
        {
            _socialService?.AcceptFriendRequest("p_sunny", (s, err) => { });
            _socialService?.AcceptFriendRequest("p_green", (s, err) => { });
        }
        public void DevVisitMockTown() => _socialManager?.StartVisitingFriend("p_sunny", (s, snap, err) => { });
        public void DevReturnFromVisit() => _socialManager?.ReturnToOwnTown();

        public void DevUnlockAdventure() => _adventureManager?.DevUnlockAdventure();
        public void DevRevealAdventureMap() => _adventureManager?.DevRevealAllMap();
        public void DevClearAdventureObstacles() => _adventureManager?.DevClearAllObstacles();
        public void DevRefillAdventureEnergy() => _adventureManager?.EnergyManager?.RefillEnergy();
        public void DevGiveAdventureResources()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.AddItem("item_stone", "Stone", ItemType.RawMaterial, 30);
                _inventoryManager.AddItem("item_wood", "Wood", ItemType.RawMaterial, 30);
                _inventoryManager.AddItem("item_clay", "Clay", ItemType.RawMaterial, 20);
                _inventoryManager.AddItem("item_ore", "Iron Ore", ItemType.RawMaterial, 10);
                _inventoryManager.AddItem("item_rare_crystal", "Rare Crystal", ItemType.RawMaterial, 5);
                _inventoryManager.AddItem("item_brick", "Brick", ItemType.ManufacturedGood, 10);
            }
        }

        public void ToggleDevMenu()
        {
            _showDevMenu = !_showDevMenu;
        }

        public void DevUnlockAllLand()
        {
            _expansionManager?.UnlockAllZonesDev();
        }

        public void DevResetMap()
        {
            _placementManager?.Clear();
            if (_roadManager != null)
            {
                var roads = _roadManager.GetRoadTiles();
                foreach (var r in roads)
                {
                    _roadManager.RemoveRoad(r);
                }
            }
        }

        public void DevGiveCoinsAndGems()
        {
            _economyManager?.EarnCoins(5000);
            _economyManager?.EarnGems(100);
        }

        public void DevGiveMaterials()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.AddItem("wood", "Wood", ItemType.RawMaterial, 50);
                _inventoryManager.AddItem("stone", "Stone", ItemType.RawMaterial, 50);
                _inventoryManager.AddItem("crop_wheat", "Wheat", ItemType.Crop, 50);
                _inventoryManager.AddItem("crop_sugarcane", "Sugarcane", ItemType.Crop, 50);
                _inventoryManager.AddItem("item_flour", "Flour", ItemType.ManufacturedGood, 20);
                _inventoryManager.AddItem("item_sugar", "Sugar", ItemType.ManufacturedGood, 20);
                _inventoryManager.AddItem("item_bread", "Bread", ItemType.ManufacturedGood, 20);
            }
        }

        public void DevGiveSeeds()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.AddItem("seed_wheat", "Wheat Seeds", ItemType.Seed, 20);
                _inventoryManager.AddItem("seed_corn", "Corn Seeds", ItemType.Seed, 20);
                _inventoryManager.AddItem("seed_carrot", "Carrot Seeds", ItemType.Seed, 20);
                _inventoryManager.AddItem("seed_sugarcane", "Sugarcane Seeds", ItemType.Seed, 20);
                _inventoryManager.AddItem("seed_tomato", "Tomato Seeds", ItemType.Seed, 20);
            }
        }

        public void DevAddResident()
        {
            _populationManager?.DevAddResident();
        }

        public void DevRecalculatePopulationAndHappiness()
        {
            _populationManager?.RecalculatePopulationAndAssignments();
            _accessibilityService?.RecalculateAllBuildingAccessibility();
        }

        public void DevSetHappinessScore(int score)
        {
            _populationManager?.GetHappinessManager()?.SetOverrideScore(score);
        }

        public void DevInstantGrowAllFields()
        {
            if (_farmManager != null)
            {
                var fields = _farmManager.GetAllFields();
                foreach (var field in fields)
                {
                    _farmManager.DevInstantGrow(field.FieldId);
                }
            }
        }

        public void DevInstantCompleteAllConstructions()
        {
            if (_buildingManager != null)
            {
                var buildings = _buildingManager.GetAllBuildings();
                foreach (var b in buildings)
                {
                    _buildingManager.DevInstantCompleteConstruction(b.InstanceId);
                }
            }
        }

        public void DevInstantCompleteAllProductionJobs()
        {
            if (_productionManager != null)
            {
                var prodBuildings = _productionManager.GetAllProductionBuildings();
                foreach (var pb in prodBuildings)
                {
                    _productionManager.DevInstantCompleteCurrentJob(pb.BuildingInstanceId);
                }
            }
        }

        public void DevGenerateNewOrder()
        {
            _orderManager?.DevGenerateNewOrder();
        }

        public void DevFulfillFirstOrder()
        {
            if (_orderManager != null)
            {
                var orders = _orderManager.GetActiveOrders();
                if (orders != null && orders.Count > 0)
                {
                    foreach (var req in orders[0].Requirements)
                    {
                        _inventoryManager.AddItem(req.ItemId, req.ItemId, ItemType.ManufacturedGood, req.Quantity);
                    }
                    _orderManager.FulfillOrder(orders[0].OrderId);
                }
            }
        }

        public bool DevPlaceTestObject(Vector2Int origin)
        {
            if (_placementManager == null) return false;
            var footprint = new ObjectFootprint(2, 2);
            string id = "test_bldg_" + System.Guid.NewGuid().ToString().Substring(0, 5);
            return _placementManager.TryPlaceObject(id, "test_house", origin, footprint, RotationAngle.Deg0, out _);
        }

        public bool DevRemoveObjectAt(Vector2Int origin)
        {
            if (_grid == null || _placementManager == null) return false;
            TileData tile = _grid.GetTile(origin);
            if (tile != null && tile.IsOccupied && !string.IsNullOrEmpty(tile.OccupyingObjectId))
            {
                return _placementManager.RemoveObject(tile.OccupyingObjectId);
            }
            return false;
        }
    }
}
