using UnityEngine;
using Game.World;
using Game.Save;

namespace Game.UI
{
    public class DevDebugToolsHandler : MonoBehaviour
    {
        private WorldGrid _grid;
        private ObjectPlacementManager _placementManager;
        private RoadManager _roadManager;
        private LandExpansionManager _expansionManager;
        private LocalSaveSystem _saveSystem;

        private bool _showDevMenu = false;

        public void Initialize(
            WorldGrid grid,
            ObjectPlacementManager placementManager,
            RoadManager roadManager,
            LandExpansionManager expansionManager,
            LocalSaveSystem saveSystem)
        {
            _grid = grid;
            _placementManager = placementManager;
            _roadManager = roadManager;
            _expansionManager = expansionManager;
            _saveSystem = saveSystem;
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
