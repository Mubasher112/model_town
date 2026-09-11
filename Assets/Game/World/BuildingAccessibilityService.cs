using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Buildings;

namespace Game.World
{
    public class BuildingAccessibilityService
    {
        private readonly BuildingManager _buildingManager;
        private readonly RoadManager _roadManager;

        public event Action OnAccessibilityUpdated;

        public BuildingAccessibilityService(BuildingManager buildingManager, RoadManager roadManager)
        {
            _buildingManager = buildingManager ?? throw new ArgumentNullException(nameof(buildingManager));
            _roadManager = roadManager ?? throw new ArgumentNullException(nameof(roadManager));

            if (_roadManager != null)
            {
                _roadManager.OnRoadChanged += HandleRoadChanged;
            }

            if (_buildingManager != null)
            {
                _buildingManager.OnBuildingStateChanged += HandleBuildingStateChanged;
            }
        }

        private void HandleRoadChanged(Vector2Int coord, bool isRoad)
        {
            RecalculateAllBuildingAccessibility();
        }

        private void HandleBuildingStateChanged(BuildingInstance b)
        {
            RecalculateAllBuildingAccessibility();
        }

        public bool IsBuildingAccessible(BuildingInstance building)
        {
            if (building == null || _roadManager == null) return false;

            var config = BuildingLibrary.GetBuilding(building.BuildingId);
            if (config == null || !config.RequiresRoadAccess)
            {
                return true;
            }

            var footprint = new ObjectFootprint(config.Width, config.Height);
            return _roadManager.IsConnectedToNetwork(building.GridPosition, footprint);
        }

        public (int totalRequired, int accessibleCount, int inaccessibleCount) RecalculateAllBuildingAccessibility()
        {
            int totalRequired = 0;
            int accessibleCount = 0;
            int inaccessibleCount = 0;

            if (_buildingManager == null) return (0, 0, 0);

            var buildings = _buildingManager.GetAllBuildings();
            foreach (var b in buildings)
            {
                if (b.State == BuildingState.Completed)
                {
                    var config = BuildingLibrary.GetBuilding(b.BuildingId);
                    if (config != null && config.RequiresRoadAccess)
                    {
                        totalRequired++;
                        bool accessible = IsBuildingAccessible(b);
                        if (accessible)
                        {
                            accessibleCount++;
                        }
                        else
                        {
                            inaccessibleCount++;
                        }
                    }
                }
            }

            OnAccessibilityUpdated?.Invoke();
            return (totalRequired, accessibleCount, inaccessibleCount);
        }
    }
}
