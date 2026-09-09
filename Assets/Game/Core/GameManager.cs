using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Game.Player;
using Game.Economy;
using Game.Inventory;
using Game.World;
using Game.Farming;
using Game.Buildings;
using Game.Data;
using Game.Save;
using Game.Platforms;
using Game.UI;
using Game.Camera;
using Game.Services;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerProfile PlayerProfile { get; private set; }
        public EconomyManager EconomyManager { get; private set; }
        public InventoryManager InventoryManager { get; private set; }
        public WorldGrid WorldGrid { get; private set; }
        public ObjectPlacementManager PlacementManager { get; private set; }
        public RoadManager RoadManager { get; private set; }
        public LandExpansionManager ExpansionManager { get; private set; }
        public StandardGameTimeService TimeService { get; private set; }
        public FarmManager FarmManager { get; private set; }
        public BuildingManager BuildingManager { get; private set; }
        public LocalSaveSystem SaveSystem { get; private set; }
        public LocalMockPlatformServices PlatformServices { get; private set; }

        [SerializeField] private MobileUIManager uiManager;
        [SerializeField] private TileSelectionHandler tileSelectionHandler;
        [SerializeField] private DevDebugToolsHandler devToolsHandler;
        [SerializeField] private FarmingUIController farmingUIController;
        [SerializeField] private BuildMenuUIController buildMenuUIController;
        [SerializeField] private BuildingInfoUIController buildingInfoUIController;
        [SerializeField] private MobileCameraController cameraController;

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "player_save_v4.json");

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeGame()
        {
            PlatformServices = PlatformServiceFactory.CreatePlatformServices();
            SaveSystem = new LocalSaveSystem(SaveFilePath);
            TimeService = new StandardGameTimeService();

            LoadGame();

            if (uiManager != null)
            {
                uiManager.BindPlayerProfile(PlayerProfile);
            }

            if (tileSelectionHandler != null)
            {
                tileSelectionHandler.Initialize(WorldGrid);
                tileSelectionHandler.OnTileSelected += HandleTileSelectedForInteractions;
            }

            if (farmingUIController != null)
            {
                farmingUIController.Initialize(FarmManager, InventoryManager, PlayerProfile);
            }

            if (buildMenuUIController != null)
            {
                buildMenuUIController.Initialize(BuildingManager, PlayerProfile, EconomyManager, InventoryManager);
            }

            if (buildingInfoUIController != null)
            {
                buildingInfoUIController.Initialize(BuildingManager, EconomyManager, PlayerProfile);
            }

            if (devToolsHandler != null)
            {
                devToolsHandler.Initialize(WorldGrid, PlacementManager, RoadManager, ExpansionManager, SaveSystem, FarmManager, InventoryManager, BuildingManager, EconomyManager);
            }

            if (cameraController != null)
            {
                cameraController.SetBoundsFromGrid(WorldGrid);
            }
        }

        private void HandleTileSelectedForInteractions(Vector2Int gridPos, TileData tile)
        {
            if (tile == null) return;

            if (tile.IsOccupied && !string.IsNullOrEmpty(tile.OccupyingObjectId))
            {
                // Check if it's a field
                if (FarmManager != null)
                {
                    var field = FarmManager.GetField(tile.OccupyingObjectId);
                    if (field != null)
                    {
                        if (farmingUIController != null) farmingUIController.OnFieldSelected(field);
                        if (buildingInfoUIController != null) buildingInfoUIController.ClosePanel();
                        return;
                    }
                }

                // Check if it's a building
                if (BuildingManager != null)
                {
                    var b = BuildingManager.GetBuilding(tile.OccupyingObjectId);
                    if (b != null)
                    {
                        if (buildingInfoUIController != null) buildingInfoUIController.OnBuildingSelected(b);
                        if (farmingUIController != null) farmingUIController.OnFieldSelected(null);
                        return;
                    }
                }
            }

            if (farmingUIController != null) farmingUIController.OnFieldSelected(null);
            if (buildingInfoUIController != null) buildingInfoUIController.ClosePanel();
        }

        private void Update()
        {
            if (TimeService != null)
            {
                TimeService.UpdateCurrentTime();
            }

            if (FarmManager != null)
            {
                FarmManager.UpdateAllFieldStates();
            }

            if (BuildingManager != null)
            {
                BuildingManager.UpdateAllBuildingStates();
            }
        }

        public bool PlaceNewField(Vector2Int origin)
        {
            string fieldId = "field_" + System.Guid.NewGuid().ToString().Substring(0, 6);
            var footprint = new ObjectFootprint(1, 1);

            if (PlacementManager.TryPlaceObject(fieldId, "field", origin, footprint, RotationAngle.Deg0, out _))
            {
                var fieldInstance = new FieldInstance(fieldId, origin, 1, 1);
                FarmManager.RegisterField(fieldInstance);
                return true;
            }
            return false;
        }

        public void SaveGame()
        {
            var saveData = new SaveData
            {
                Version = LocalSaveSystem.CurrentSaveVersion,
                PlayerProfile = PlayerProfile,
                InventoryItems = InventoryManager.GetAllItems(),
                MapWidth = WorldGrid.Width,
                MapHeight = WorldGrid.Height,
                UnlockedZoneIds = ExpansionManager.GetUnlockedZoneIds()
            };

            var placedObjs = PlacementManager.GetAllPlacedObjects();
            foreach (var p in placedObjs)
            {
                saveData.PlacedObjects.Add(new SavedPlacedObject
                {
                    ObjectId = p.InstanceId,
                    ObjectTypeId = p.ObjectTypeId,
                    X = p.Origin.x,
                    Y = p.Origin.y,
                    BaseWidth = p.Footprint.BaseWidth,
                    BaseHeight = p.Footprint.BaseHeight,
                    RotationDegrees = (int)p.Rotation
                });
            }

            var roads = RoadManager.GetRoadTiles();
            foreach (var r in roads)
            {
                saveData.RoadTiles.Add(new SavedRoadTile(r.x, r.y));
            }

            var fields = FarmManager.GetAllFields();
            foreach (var f in fields)
            {
                saveData.Fields.Add(new SavedField
                {
                    FieldId = f.FieldId,
                    X = f.GridPosition.x,
                    Y = f.GridPosition.y,
                    Width = f.Width,
                    Height = f.Height,
                    State = (int)f.State,
                    CurrentCropId = f.CurrentCropId,
                    PlantedUtcTicks = f.PlantedUtcTicks
                });
            }

            var buildings = BuildingManager.GetAllBuildings();
            foreach (var b in buildings)
            {
                var bConfig = BuildingLibrary.GetBuilding(b.BuildingId);
                saveData.Buildings.Add(new SavedBuilding
                {
                    InstanceId = b.InstanceId,
                    BuildingId = b.BuildingId,
                    X = b.GridPosition.x,
                    Y = b.GridPosition.y,
                    BaseWidth = bConfig != null ? bConfig.Width : 1,
                    BaseHeight = bConfig != null ? bConfig.Height : 1,
                    RotationDegrees = (int)b.Rotation,
                    Level = b.Level,
                    State = (int)b.State,
                    ConstructionStartUtcTicks = b.ConstructionStartUtcTicks,
                    UpgradeStartUtcTicks = b.UpgradeStartUtcTicks,
                    CompletionXpAwarded = b.CompletionXpAwarded
                });
            }

            SaveSystem.Save(saveData);
        }

        public void LoadGame()
        {
            SaveData saveData = SaveSystem.Load();
            PlayerProfile = saveData.PlayerProfile ?? new PlayerProfile();
            EconomyManager = new EconomyManager(PlayerProfile);
            InventoryManager = new InventoryManager();

            if (saveData.InventoryItems != null)
            {
                foreach (var item in saveData.InventoryItems)
                {
                    InventoryManager.AddItem(item.ItemId, item.ItemName, item.Type, item.Quantity);
                }
            }

            int width = saveData.MapWidth > 0 ? saveData.MapWidth : 30;
            int height = saveData.MapHeight > 0 ? saveData.MapHeight : 30;
            WorldGrid = new WorldGrid(width, height);

            PlacementManager = new ObjectPlacementManager(WorldGrid);
            RoadManager = new RoadManager(WorldGrid);
            ExpansionManager = new LandExpansionManager(WorldGrid);
            FarmManager = new FarmManager(InventoryManager, PlayerProfile, TimeService);
            BuildingManager = new BuildingManager(PlacementManager, EconomyManager, InventoryManager, PlayerProfile, TimeService);

            if (saveData.UnlockedZoneIds != null && saveData.UnlockedZoneIds.Count > 0)
            {
                ExpansionManager.LoadUnlockedZones(saveData.UnlockedZoneIds);
            }

            if (saveData.RoadTiles != null)
            {
                var roadCoords = new List<Vector2Int>();
                foreach (var r in saveData.RoadTiles)
                {
                    roadCoords.Add(new Vector2Int(r.X, r.Y));
                }
                RoadManager.LoadRoads(roadCoords);
            }

            if (saveData.PlacedObjects != null)
            {
                foreach (var obj in saveData.PlacedObjects)
                {
                    var footprint = new ObjectFootprint(obj.BaseWidth, obj.BaseHeight);
                    var rotation = (RotationAngle)obj.RotationDegrees;
                    PlacementManager.TryPlaceObject(obj.ObjectId, obj.ObjectTypeId, new Vector2Int(obj.X, obj.Y), footprint, rotation, out _);
                }
            }

            if (saveData.Fields != null)
            {
                foreach (var sf in saveData.Fields)
                {
                    var fieldInstance = new FieldInstance(sf.FieldId, new Vector2Int(sf.X, sf.Y), sf.Width, sf.Height)
                    {
                        State = (FieldState)sf.State,
                        CurrentCropId = sf.CurrentCropId,
                        PlantedUtcTicks = sf.PlantedUtcTicks
                    };

                    if (PlacementManager.GetPlacedObject(sf.FieldId) == null)
                    {
                        var footprint = new ObjectFootprint(sf.Width, sf.Height);
                        PlacementManager.TryPlaceObject(sf.FieldId, "field", new Vector2Int(sf.X, sf.Y), footprint, RotationAngle.Deg0, out _);
                    }

                    FarmManager.RegisterField(fieldInstance);
                }
            }

            if (saveData.Buildings != null)
            {
                foreach (var sb in saveData.Buildings)
                {
                    var bConfig = BuildingLibrary.GetBuilding(sb.BuildingId);
                    int w = bConfig != null ? bConfig.Width : sb.BaseWidth;
                    int h = bConfig != null ? bConfig.Height : sb.BaseHeight;

                    var bInstance = new BuildingInstance(sb.InstanceId, sb.BuildingId, new Vector2Int(sb.X, sb.Y), (RotationAngle)sb.RotationDegrees)
                    {
                        Level = sb.Level,
                        State = (BuildingState)sb.State,
                        ConstructionStartUtcTicks = sb.ConstructionStartUtcTicks,
                        UpgradeStartUtcTicks = sb.UpgradeStartUtcTicks,
                        CompletionXpAwarded = sb.CompletionXpAwarded
                    };

                    if (PlacementManager.GetPlacedObject(sb.InstanceId) == null)
                    {
                        var footprint = new ObjectFootprint(w, h);
                        PlacementManager.TryPlaceObject(sb.InstanceId, sb.BuildingId, new Vector2Int(sb.X, sb.Y), footprint, (RotationAngle)sb.RotationDegrees, out _);
                    }

                    BuildingManager.RegisterBuilding(bInstance);
                }
            }
        }

        private void OnDestroy()
        {
            if (tileSelectionHandler != null)
            {
                tileSelectionHandler.OnTileSelected -= HandleTileSelectedForInteractions;
            }
            SaveGame();
        }
    }
}
