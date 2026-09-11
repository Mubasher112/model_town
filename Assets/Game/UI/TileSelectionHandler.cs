using System;
using UnityEngine;
using UnityEngine.UI;
using Game.World;
using Game.Input;

namespace Game.UI
{
    public class TileSelectionHandler : MonoBehaviour
    {
        [SerializeField] private Text tileInfoText;
        [SerializeField] private GameObject selectionIndicator;

        private WorldGrid _grid;
        private Vector2Int _selectedTile = new Vector2Int(-1, -1);

        public event Action<Vector2Int, TileData> OnTileSelected;

        public void Initialize(WorldGrid grid)
        {
            _grid = grid;
            if (MobileInputManager.Instance != null)
            {
                MobileInputManager.Instance.OnTap += HandleScreenTap;
            }
        }

        private void OnDestroy()
        {
            if (MobileInputManager.Instance != null)
            {
                MobileInputManager.Instance.OnTap -= HandleScreenTap;
            }
        }

        private void HandleScreenTap(Vector2 screenPos)
        {
            if (_grid == null) return;

            Vector3 cameraPos = UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : Vector3.zero;
            float zoom = 8f;

            Vector2Int tappedGridPos = _grid.ScreenToGrid(screenPos, cameraPos, zoom);

            if (_grid.IsValidCoordinate(tappedGridPos))
            {
                SelectTile(tappedGridPos);
            }
        }

        public void SelectTile(Vector2Int gridPos)
        {
            if (_grid == null || !_grid.IsValidCoordinate(gridPos)) return;

            _selectedTile = gridPos;
            TileData tile = _grid.GetTile(gridPos);

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
                Vector3 worldPos = _grid.GridToWorld(gridPos);
                selectionIndicator.transform.position = worldPos;
            }

            if (tileInfoText != null && tile != null)
            {
                var terrainConfig = TerrainConfigLibrary.GetConfig(tile.Type);
                tileInfoText.text = $"Pos: ({tile.Position.x}, {tile.Position.y}) | Type: {terrainConfig.Name}\n" +
                                    $"Buildable: {tile.IsBuildable} | Occupied: {tile.IsOccupied} | Locked: {tile.IsLocked}";
            }

            OnTileSelected?.Invoke(gridPos, tile);
        }

        public Vector2Int GetSelectedTilePos() => _selectedTile;
    }
}
