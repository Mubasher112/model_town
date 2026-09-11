using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    public class WorldRenderer : MonoBehaviour
    {
        private WorldGrid _grid;
        private ObjectPlacementManager _placementManager;
        private RoadManager _roadManager;

        private readonly Dictionary<Vector2Int, GameObject> _renderedTiles = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<string, GameObject> _renderedObjects = new Dictionary<string, GameObject>();

        public void Initialize(WorldGrid grid, ObjectPlacementManager placementManager, RoadManager roadManager)
        {
            _grid = grid;
            _placementManager = placementManager;
            _roadManager = roadManager;

            if (_grid != null)
            {
                _grid.OnTileChanged += UpdateTileVisual;
                RenderFullGrid();
            }

            if (_placementManager != null)
            {
                _placementManager.OnObjectPlaced += RenderPlacedObject;
                _placementManager.OnObjectRemoved += DestroyPlacedObjectVisual;
            }
        }

        private void OnDestroy()
        {
            if (_grid != null)
            {
                _grid.OnTileChanged -= UpdateTileVisual;
            }

            if (_placementManager != null)
            {
                _placementManager.OnObjectPlaced -= RenderPlacedObject;
                _placementManager.OnObjectRemoved -= DestroyPlacedObjectVisual;
            }
        }

        public void RenderFullGrid()
        {
            ClearAllRendered();

            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    TileData tile = _grid.GetTile(pos);
                    CreateTileVisual(pos, tile);
                }
            }
        }

        private void CreateTileVisual(Vector2Int pos, TileData tile)
        {
            if (tile == null) return;

            GameObject tileObj = new GameObject($"Tile_{pos.x}_{pos.y}");
            tileObj.transform.parent = transform;
            tileObj.transform.position = _grid.GridToWorld(pos);

            var sr = tileObj.GetComponent<SpriteRenderer>();
            var terrainConfig = TerrainConfigLibrary.GetConfig(tile.Type);

            if (tile.IsLocked)
            {
                sr.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);
            }
            else
            {
                sr.color = GetColorFromHex(terrainConfig.ColorHex);
            }

            sr.sortingOrder = -(pos.x + pos.y);
            _renderedTiles[pos] = tileObj;
        }

        private void UpdateTileVisual(Vector2Int pos, TileData tile)
        {
            if (_renderedTiles.TryGetValue(pos, out var tileObj))
            {
                Destroy(tileObj);
                _renderedTiles.Remove(pos);
            }
            CreateTileVisual(pos, tile);
        }

        private void RenderPlacedObject(PlacedObjectInstance instance)
        {
            if (instance == null) return;

            GameObject obj = new GameObject($"Object_{instance.InstanceId}");
            obj.transform.parent = transform;
            obj.transform.position = _grid.GridToWorld(instance.Origin);

            var sr = obj.GetComponent<SpriteRenderer>();
            sr.color = new Color(0.8f, 0.4f, 0.1f, 1.0f); // Default building orange
            sr.sortingOrder = -(instance.Origin.x + instance.Origin.y) + 10;

            _renderedObjects[instance.InstanceId] = obj;
        }

        private void DestroyPlacedObjectVisual(string instanceId)
        {
            if (_renderedObjects.TryGetValue(instanceId, out var obj))
            {
                Destroy(obj);
                _renderedObjects.Remove(instanceId);
            }
        }

        private void ClearAllRendered()
        {
            foreach (var kvp in _renderedTiles)
            {
                Destroy(kvp.Value);
            }
            _renderedTiles.Clear();

            foreach (var kvp in _renderedObjects)
            {
                Destroy(kvp.Value);
            }
            _renderedObjects.Clear();
        }

        private Color GetColorFromHex(string hex)
        {
            if (hex == "#55A630") return new Color(0.33f, 0.65f, 0.18f); // Grass green
            if (hex == "#8C5E3B") return new Color(0.55f, 0.36f, 0.23f); // Soil brown
            if (hex == "#2B6CB0") return new Color(0.16f, 0.42f, 0.69f); // Water blue
            if (hex == "#718096") return new Color(0.44f, 0.50f, 0.58f); // Road gray
            if (hex == "#4A5568") return new Color(0.29f, 0.33f, 0.40f); // Rock slate
            if (hex == "#2F855A") return new Color(0.18f, 0.52f, 0.35f); // Tree dark green
            return new Color(0.6f, 0.6f, 0.6f);
        }
    }
}
