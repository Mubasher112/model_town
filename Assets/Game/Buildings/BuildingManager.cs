using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Economy;
using Game.Inventory;
using Game.Player;
using Game.World;
using Game.Services;

namespace Game.Buildings
{
    public enum BuildingOperationResult
    {
        Success,
        InvalidBuilding,
        CannotAffordCoins,
        CannotAffordGems,
        MissingMaterials,
        BuildingLocked,
        PlacementInvalid,
        AlreadyMaxLevel,
        InvalidState
    }

    public class BuildingManager
    {
        private readonly Dictionary<string, BuildingInstance> _buildings = new Dictionary<string, BuildingInstance>();
        private readonly ObjectPlacementManager _placementManager;
        private readonly EconomyManager _economyManager;
        private readonly InventoryManager _inventoryManager;
        private readonly PlayerProfile _playerProfile;
        private readonly StandardGameTimeService _timeService;

        public event Action<BuildingInstance> OnBuildingStateChanged;
        public event Action<string> OnNotificationMessage;
        public event Action OnTownStatsChanged;

        public int TotalPopulationCapacity { get; private set; }
        public int CurrentPopulation { get; set; } = 0;

        public BuildingManager(
            ObjectPlacementManager placementManager,
            EconomyManager economyManager,
            InventoryManager inventoryManager,
            PlayerProfile playerProfile,
            StandardGameTimeService timeService)
        {
            _placementManager = placementManager ?? throw new ArgumentNullException(nameof(placementManager));
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        public void RegisterBuilding(BuildingInstance building)
        {
            if (building == null) return;
            _buildings[building.InstanceId] = building;
            UpdateSingleBuildingState(building);
            RecalculateTownCapacities();
        }

        public BuildingInstance GetBuilding(string instanceId)
        {
            return _buildings.TryGetValue(instanceId, out var b) ? b : null;
        }

        public List<BuildingInstance> GetAllBuildings()
        {
            return new List<BuildingInstance>(_buildings.Values);
        }

        public void UpdateSingleBuildingState(BuildingInstance b)
        {
            if (b == null) return;
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            var config = BuildingLibrary.GetBuilding(b.BuildingId);
            if (b.CheckAndUpdateState(nowTicks, config))
            {
                if (b.State == BuildingState.Completed && !b.CompletionXpAwarded)
                {
                    b.CompletionXpAwarded = true;
                    if (config != null)
                    {
                        _playerProfile.AddXP(config.XpReward);
                    }
                }
                RecalculateTownCapacities();
                OnBuildingStateChanged?.Invoke(b);
            }
        }

        public void UpdateAllBuildingStates()
        {
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            foreach (var b in _buildings.Values)
            {
                var config = BuildingLibrary.GetBuilding(b.BuildingId);
                if (b.CheckAndUpdateState(nowTicks, config))
                {
                    if (b.State == BuildingState.Completed && !b.CompletionXpAwarded)
                    {
                        b.CompletionXpAwarded = true;
                        if (config != null)
                        {
                            _playerProfile.AddXP(config.XpReward);
                        }
                    }
                    RecalculateTownCapacities();
                    OnBuildingStateChanged?.Invoke(b);
                }
            }
        }

        public BuildingOperationResult StartConstruction(string buildingId, Vector2Int origin, RotationAngle rotation, out BuildingInstance instance)
        {
            instance = null;
            var config = BuildingLibrary.GetBuilding(buildingId);
            if (config == null) return BuildingOperationResult.InvalidBuilding;

            if (_playerProfile.Level < config.UnlockLevel)
            {
                OnNotificationMessage?.Invoke($"Requires Level {config.UnlockLevel}");
                return BuildingOperationResult.BuildingLocked;
            }

            // Check coin cost
            if (!_economyManager.CanAffordCoins(config.BuildCostCoins))
            {
                OnNotificationMessage?.Invoke("Not enough coins");
                return BuildingOperationResult.CannotAffordCoins;
            }

            // Check material cost
            if (config.RequiredMaterials != null)
            {
                foreach (var mat in config.RequiredMaterials)
                {
                    if (_inventoryManager.GetQuantity(mat.ItemId) < mat.Quantity)
                    {
                        OnNotificationMessage?.Invoke($"Missing required materials: {mat.ItemId}");
                        return BuildingOperationResult.MissingMaterials;
                    }
                }
            }

            // Check placement validity
            var footprint = new ObjectFootprint(config.Width, config.Height);
            string instanceId = "bldg_" + Guid.NewGuid().ToString().Substring(0, 6);

            if (!_placementManager.TryPlaceObject(instanceId, buildingId, origin, footprint, rotation, out _))
            {
                OnNotificationMessage?.Invoke("Invalid placement location");
                return BuildingOperationResult.PlacementInvalid;
            }

            // Deduct resources once
            _economyManager.SpendCoins(config.BuildCostCoins);
            if (config.RequiredMaterials != null)
            {
                foreach (var mat in config.RequiredMaterials)
                {
                    _inventoryManager.RemoveItem(mat.ItemId, mat.Quantity);
                }
            }

            _timeService.UpdateCurrentTime();
            instance = new BuildingInstance(instanceId, buildingId, origin, rotation)
            {
                ConstructionStartUtcTicks = _timeService.CurrentUtcTicks,
                State = BuildingState.UnderConstruction
            };

            // If 0 construction time (e.g. tree/decorations), complete instantly
            if (config.ConstructionTimeSeconds <= 0f)
            {
                instance.State = BuildingState.Completed;
                instance.CompletionXpAwarded = true;
                _playerProfile.AddXP(config.XpReward);
            }

            RegisterBuilding(instance);
            OnBuildingStateChanged?.Invoke(instance);
            OnNotificationMessage?.Invoke($"{config.Name} construction started!");
            return BuildingOperationResult.Success;
        }

        public BuildingOperationResult StartUpgrade(string instanceId)
        {
            var b = GetBuilding(instanceId);
            if (b == null || b.State != BuildingState.Completed) return BuildingOperationResult.InvalidState;

            var config = BuildingLibrary.GetBuilding(b.BuildingId);
            if (config == null) return BuildingOperationResult.InvalidBuilding;

            if (b.Level >= config.MaxUpgradeLevel)
            {
                OnNotificationMessage?.Invoke("Building is already at max level");
                return BuildingOperationResult.AlreadyMaxLevel;
            }

            if (!_economyManager.CanAffordCoins(config.UpgradeCostCoins))
            {
                OnNotificationMessage?.Invoke("Not enough coins to upgrade");
                return BuildingOperationResult.CannotAffordCoins;
            }

            _economyManager.SpendCoins(config.UpgradeCostCoins);

            _timeService.UpdateCurrentTime();
            b.UpgradeStartUtcTicks = _timeService.CurrentUtcTicks;
            b.State = BuildingState.Upgrading;

            if (config.UpgradeTimeSeconds <= 0f)
            {
                b.State = BuildingState.Completed;
                b.Level++;
                RecalculateTownCapacities();
            }

            OnBuildingStateChanged?.Invoke(b);
            OnNotificationMessage?.Invoke($"{config.Name} upgrade started!");
            return BuildingOperationResult.Success;
        }

        public bool RemoveBuilding(string instanceId)
        {
            var b = GetBuilding(instanceId);
            if (b == null) return false;

            if (_placementManager.RemoveObject(instanceId))
            {
                _buildings.Remove(instanceId);
                RecalculateTownCapacities();
                OnNotificationMessage?.Invoke("Building removed");
                return true;
            }
            return false;
        }

        public void RecalculateTownCapacities()
        {
            int popCap = 0;
            int storageBonus = 0;

            foreach (var b in _buildings.Values)
            {
                if (b.State == BuildingState.Completed)
                {
                    var config = BuildingLibrary.GetBuilding(b.BuildingId);
                    if (config != null)
                    {
                        int lvlMultiplier = b.Level;
                        popCap += config.PopulationCapacity + (lvlMultiplier - 1) * config.UpgradePopulationBonus;
                        storageBonus += config.StorageCapacityBonus + (lvlMultiplier - 1) * config.UpgradeStorageBonus;
                    }
                }
            }

            TotalPopulationCapacity = popCap;
            _inventoryManager.MaxCapacity = 100 + storageBonus; // Base 100 + Barn upgrades
            OnTownStatsChanged?.Invoke();
        }

        public void DevInstantCompleteConstruction(string instanceId)
        {
            var b = GetBuilding(instanceId);
            if (b != null)
            {
                var config = BuildingLibrary.GetBuilding(b.BuildingId);
                if (b.State == BuildingState.UnderConstruction)
                {
                    b.State = BuildingState.Completed;
                    if (!b.CompletionXpAwarded && config != null)
                    {
                        b.CompletionXpAwarded = true;
                        _playerProfile.AddXP(config.XpReward);
                    }
                }
                else if (b.State == BuildingState.Upgrading)
                {
                    b.State = BuildingState.Completed;
                    b.Level++;
                }
                RecalculateTownCapacities();
                OnBuildingStateChanged?.Invoke(b);
            }
        }
    }
}
