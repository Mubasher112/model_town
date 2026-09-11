using System.Collections.Generic;
using UnityEngine;
using Game.Residents;
using Game.Buildings;

namespace Game.World
{
    public class ResidentWorldRenderer : MonoBehaviour
    {
        private PopulationManager _populationManager;
        private BuildingManager _buildingManager;
        private WorldGrid _grid;

        private readonly Dictionary<string, GameObject> _renderedNPCs = new Dictionary<string, GameObject>();

        public void Initialize(PopulationManager populationManager, BuildingManager buildingManager, WorldGrid grid)
        {
            _populationManager = populationManager;
            _buildingManager = buildingManager;
            _grid = grid;

            if (_populationManager != null)
            {
                _populationManager.OnPopulationUpdated += RefreshWorldNPCs;
            }

            RefreshWorldNPCs();
        }

        private void OnDestroy()
        {
            if (_populationManager != null)
            {
                _populationManager.OnPopulationUpdated -= RefreshWorldNPCs;
            }
        }

        public void RefreshWorldNPCs()
        {
            ClearNPCVisuals();

            if (_populationManager == null || _buildingManager == null || _grid == null) return;

            var residents = _populationManager.GetAllResidents();
            foreach (var r in residents)
            {
                if (!string.IsNullOrEmpty(r.AssignedHouseInstanceId))
                {
                    var house = _buildingManager.GetBuilding(r.AssignedHouseInstanceId);
                    if (house != null)
                    {
                        GameObject npcObj = new GameObject($"NPC_{r.ResidentId}");
                        npcObj.transform.parent = transform;

                        Vector3 houseWorldPos = _grid.GridToWorld(house.GridPosition);
                        // Offset NPC slightly near house
                        npcObj.transform.position = houseWorldPos + new Vector3(0.2f, -0.2f, 0f);

                        var sr = npcObj.GetComponent<SpriteRenderer>();
                        sr.color = new Color(0.9f, 0.8f, 0.2f, 1f); // Bright yellow placeholder
                        sr.sortingOrder = -(house.GridPosition.x + house.GridPosition.y) + 15;

                        _renderedNPCs[r.ResidentId] = npcObj;
                    }
                }
            }
        }

        private void ClearNPCVisuals()
        {
            foreach (var kvp in _renderedNPCs)
            {
                Destroy(kvp.Value);
            }
            _renderedNPCs.Clear();
        }
    }
}
