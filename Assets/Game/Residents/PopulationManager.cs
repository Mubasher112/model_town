using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Buildings;
using Game.Player;
using Game.Services;

namespace Game.Residents
{
    public class PopulationManager
    {
        private readonly Dictionary<string, ResidentInstance> _residents = new Dictionary<string, ResidentInstance>();
        private readonly BuildingManager _buildingManager;
        private readonly PlayerProfile _playerProfile;
        private readonly StandardGameTimeService _timeService;
        private readonly HappinessManager _happinessManager;

        public event Action OnPopulationUpdated;
        public event Action<string> OnNotificationMessage;

        public PopulationManager(
            BuildingManager buildingManager,
            PlayerProfile playerProfile,
            StandardGameTimeService timeService,
            HappinessManager happinessManager = null)
        {
            _buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _happinessManager = happinessManager ?? new HappinessManager();

            if (_buildingManager != null)
            {
                _buildingManager.OnBuildingStateChanged += HandleBuildingStateChanged;
                _buildingManager.OnBuildingRemoved += HandleBuildingRemoved;
            }
        }

        public void RegisterResident(ResidentInstance resident)
        {
            if (resident == null) return;
            _residents[resident.ResidentId] = resident;
            RecalculatePopulationAndAssignments();
        }

        public ResidentInstance GetResident(string residentId)
        {
            return _residents.TryGetValue(residentId, out var r) ? r : null;
        }

        public List<ResidentInstance> GetAllResidents()
        {
            return new List<ResidentInstance>(_residents.Values);
        }

        public HappinessManager GetHappinessManager() => _happinessManager;

        private void HandleBuildingStateChanged(BuildingInstance building)
        {
            RecalculatePopulationAndAssignments();
        }

        private void HandleBuildingRemoved(string instanceId)
        {
            RecalculatePopulationAndAssignments();
        }

        public void RecalculatePopulationAndAssignments()
        {
            var buildings = _buildingManager.GetAllBuildings();
            int totalCapacity = 0;
            int residentialHousesCount = 0;

            var housesWithCapacity = new List<(BuildingInstance house, BuildingConfig config, int capacity)>();

            foreach (var b in buildings)
            {
                if (b.State == BuildingState.Completed)
                {
                    var config = BuildingLibrary.GetBuilding(b.BuildingId);
                    if (config != null && config.Category == BuildingCategory.Residential)
                    {
                        residentialHousesCount++;
                        int houseCap = config.PopulationCapacity + (b.Level - 1) * config.UpgradePopulationBonus;
                        totalCapacity += houseCap;
                        housesWithCapacity.Add((b, config, houseCap));
                    }
                }
            }

            var houseOccupantCounts = new Dictionary<string, int>();
            foreach (var h in housesWithCapacity)
            {
                houseOccupantCounts[h.house.InstanceId] = 0;
            }

            foreach (var res in _residents.Values)
            {
                if (!string.IsNullOrEmpty(res.AssignedHouseInstanceId))
                {
                    if (houseOccupantCounts.TryGetValue(res.AssignedHouseInstanceId, out int currentOccupants))
                    {
                        var houseTuple = housesWithCapacity.Find(x => x.house.InstanceId == res.AssignedHouseInstanceId);
                        if (currentOccupants < houseTuple.capacity)
                        {
                            houseOccupantCounts[res.AssignedHouseInstanceId] = currentOccupants + 1;
                            res.State = ResidentState.AtHome;
                            continue;
                        }
                    }

                    res.AssignedHouseInstanceId = null;
                    res.State = ResidentState.Unassigned;
                }
            }

            foreach (var res in _residents.Values)
            {
                if (res.State == ResidentState.Unassigned)
                {
                    foreach (var h in housesWithCapacity)
                    {
                        if (houseOccupantCounts[h.house.InstanceId] < h.capacity)
                        {
                            res.AssignedHouseInstanceId = h.house.InstanceId;
                            res.State = ResidentState.AtHome;
                            houseOccupantCounts[h.house.InstanceId]++;
                            break;
                        }
                    }
                }
            }

            int currentPop = _residents.Count;
            if (currentPop < totalCapacity)
            {
                int needed = totalCapacity - currentPop;
                _timeService.UpdateCurrentTime();
                for (int i = 0; i < needed; i++)
                {
                    var def = ResidentLibrary.GetRandomDefinitionForLevel(_playerProfile.Level);
                    string residentId = "res_" + Guid.NewGuid().ToString().Substring(0, 6);
                    var newResident = new ResidentInstance(residentId, def.ResidentTypeId, def.DisplayName, _timeService.CurrentUtcTicks);

                    foreach (var h in housesWithCapacity)
                    {
                        if (houseOccupantCounts[h.house.InstanceId] < h.capacity)
                        {
                            newResident.AssignedHouseInstanceId = h.house.InstanceId;
                            newResident.State = ResidentState.AtHome;
                            houseOccupantCounts[h.house.InstanceId]++;
                            break;
                        }
                    }

                    _residents[residentId] = newResident;
                }

                OnNotificationMessage?.Invoke("A new resident moved in!");
            }

            _happinessManager.RecalculateHappiness(buildings, _residents.Count, totalCapacity);

            OnPopulationUpdated?.Invoke();
        }

        public PopulationStats GetPopulationStats()
        {
            var buildings = _buildingManager.GetAllBuildings();
            int totalCapacity = 0;
            int residentialHousesCount = 0;
            int occupiedHousesCount = 0;

            var houseOccupantCounts = new Dictionary<string, int>();

            foreach (var b in buildings)
            {
                if (b.State == BuildingState.Completed)
                {
                    var config = BuildingLibrary.GetBuilding(b.BuildingId);
                    if (config != null && config.Category == BuildingCategory.Residential)
                    {
                        residentialHousesCount++;
                        int houseCap = config.PopulationCapacity + (b.Level - 1) * config.UpgradePopulationBonus;
                        totalCapacity += houseCap;
                        houseOccupantCounts[b.InstanceId] = 0;
                    }
                }
            }

            int unassignedCount = 0;
            foreach (var res in _residents.Values)
            {
                if (res.State == ResidentState.Unassigned)
                {
                    unassignedCount++;
                }
                else if (!string.IsNullOrEmpty(res.AssignedHouseInstanceId) && houseOccupantCounts.ContainsKey(res.AssignedHouseInstanceId))
                {
                    houseOccupantCounts[res.AssignedHouseInstanceId]++;
                }
            }

            foreach (var count in houseOccupantCounts.Values)
            {
                if (count > 0) occupiedHousesCount++;
            }

            int availableHousingSlots = Math.Max(0, totalCapacity - _residents.Count);

            return new PopulationStats
            {
                CurrentPopulation = _residents.Count,
                TotalHousingCapacity = totalCapacity,
                AvailableHousingSlots = availableHousingSlots,
                ResidentialHousesCount = residentialHousesCount,
                OccupiedHousesCount = occupiedHousesCount,
                UnassignedResidentsCount = unassignedCount,
                HappinessScore = _happinessManager.CurrentHappinessScore,
                HappinessRating = _happinessManager.HappinessRating
            };
        }

        public void RemoveResident(string residentId)
        {
            if (_residents.ContainsKey(residentId))
            {
                _residents.Remove(residentId);
                RecalculatePopulationAndAssignments();
            }
        }

        public void DevAddResident()
        {
            _timeService.UpdateCurrentTime();
            var def = ResidentLibrary.GetRandomDefinitionForLevel(_playerProfile.Level);
            string residentId = "res_" + Guid.NewGuid().ToString().Substring(0, 6);
            var newResident = new ResidentInstance(residentId, def.ResidentTypeId, def.DisplayName, _timeService.CurrentUtcTicks);
            RegisterResident(newResident);
        }
    }
}
