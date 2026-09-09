using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    public enum TileType
    {
        Grass,
        Soil,
        Water,
        Road,
        Rock,
        Tree,
        Locked,
        Building,
        Obstacle
    }

    public class TerrainTypeConfig
    {
        public TileType Type { get; set; }
        public string Name { get; set; }
        public bool IsBuildable { get; set; }
        public bool IsWalkable { get; set; }
        public string ColorHex { get; set; }

        public TerrainTypeConfig(TileType type, string name, bool isBuildable, bool isWalkable, string colorHex)
        {
            Type = type;
            Name = name;
            IsBuildable = isBuildable;
            IsWalkable = isWalkable;
            ColorHex = colorHex;
        }
    }

    public static class TerrainConfigLibrary
    {
        private static readonly Dictionary<TileType, TerrainTypeConfig> _configs = new Dictionary<TileType, TerrainTypeConfig>
        {
            { TileType.Grass, new TerrainTypeConfig(TileType.Grass, "Grass", true, true, "#55A630") },
            { TileType.Soil, new TerrainTypeConfig(TileType.Soil, "Soil", true, true, "#8C5E3B") },
            { TileType.Water, new TerrainTypeConfig(TileType.Water, "Water", false, false, "#2B6CB0") },
            { TileType.Road, new TerrainTypeConfig(TileType.Road, "Road", false, true, "#718096") },
            { TileType.Rock, new TerrainTypeConfig(TileType.Rock, "Rock", false, false, "#4A5568") },
            { TileType.Tree, new TerrainTypeConfig(TileType.Tree, "Tree", false, false, "#2F855A") },
            { TileType.Locked, new TerrainTypeConfig(TileType.Locked, "Locked Land", false, false, "#A0AEC0") },
            { TileType.Building, new TerrainTypeConfig(TileType.Building, "Building", false, false, "#DD6B20") },
            { TileType.Obstacle, new TerrainTypeConfig(TileType.Obstacle, "Obstacle", false, false, "#742A2A") }
        };

        public static TerrainTypeConfig GetConfig(TileType type)
        {
            return _configs.TryGetValue(type, out var config) ? config : _configs[TileType.Grass];
        }
    }

    public class TileData
    {
        public Vector2Int Position { get; }
        public TileType Type { get; set; }
        public bool IsBuildable => TerrainConfigLibrary.GetConfig(Type).IsBuildable && !IsLocked;
        public bool IsWalkable => TerrainConfigLibrary.GetConfig(Type).IsWalkable && !IsLocked && !IsOccupied;
        public bool IsOccupied { get; set; }
        public bool IsLocked { get; set; }
        public string OccupyingObjectId { get; set; }

        public TileData(Vector2Int position, TileType type = TileType.Grass, bool isLocked = false)
        {
            Position = position;
            Type = type;
            IsOccupied = false;
            IsLocked = isLocked;
            OccupyingObjectId = null;
        }
    }

    public class WorldGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public float TileWidth { get; } = 1.0f;
        public float TileHeight { get; } = 0.5f;

        private TileData[,] _tiles;

        public event Action<Vector2Int, TileData> OnTileChanged;

        public WorldGrid(int width = 30, int height = 30)
        {
            Width = width;
            Height = height;
            _tiles = new TileData[width, height];

            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    bool isWaterBorder = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1);
                    TileType type = isWaterBorder ? TileType.Water : TileType.Grass;

                    // Initial unlocked play area default (e.g. inner 20x20 unlocked)
                    bool isLocked = x < 5 || x >= Width - 5 || y < 5 || y >= Height - 5;

                    _tiles[x, y] = new TileData(new Vector2Int(x, y), type, isLocked);
                }
            }

            // Populate initial town environment features (soil patch, trees, rocks, pond)
            PopulateTerrainFeatures();
        }

        private void PopulateTerrainFeatures()
        {
            // Soil patch (farm field area)
            for (int x = 8; x <= 11; x++)
            {
                for (int y = 8; y <= 11; y++)
                {
                    if (IsValidCoordinate(new Vector2Int(x, y)))
                    {
                        _tiles[x, y].Type = TileType.Soil;
                    }
                }
            }

            // Small water pond
            for (int x = 18; x <= 20; x++)
            {
                for (int y = 18; y <= 20; y++)
                {
                    if (IsValidCoordinate(new Vector2Int(x, y)))
                    {
                        _tiles[x, y].Type = TileType.Water;
                    }
                }
            }

            // Trees
            SetTileTypeIfValid(6, 15, TileType.Tree);
            SetTileTypeIfValid(7, 15, TileType.Tree);
            SetTileTypeIfValid(6, 16, TileType.Tree);
            SetTileTypeIfValid(14, 22, TileType.Tree);
            SetTileTypeIfValid(15, 22, TileType.Tree);

            // Rocks
            SetTileTypeIfValid(22, 10, TileType.Rock);
            SetTileTypeIfValid(22, 11, TileType.Rock);
            SetTileTypeIfValid(12, 21, TileType.Rock);
        }

        private void SetTileTypeIfValid(int x, int y, TileType type)
        {
            var coord = new Vector2Int(x, y);
            if (IsValidCoordinate(coord))
            {
                _tiles[x, y].Type = type;
            }
        }

        public bool IsValidCoordinate(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;
        }

        public TileData GetTile(Vector2Int coord)
        {
            if (!IsValidCoordinate(coord)) return null;
            return _tiles[coord.x, coord.y];
        }

        public void SetTileType(Vector2Int coord, TileType newType)
        {
            if (!IsValidCoordinate(coord)) return;
            var tile = _tiles[coord.x, coord.y];
            tile.Type = newType;
            OnTileChanged?.Invoke(coord, tile);
        }

        public void SetTileLocked(Vector2Int coord, bool locked)
        {
            if (!IsValidCoordinate(coord)) return;
            var tile = _tiles[coord.x, coord.y];
            tile.IsLocked = locked;
            OnTileChanged?.Invoke(coord, tile);
        }

        #region Coordinate Conversions
        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            float worldX = (gridPos.x - gridPos.y) * (TileWidth / 2f);
            float worldY = (gridPos.x + gridPos.y) * (TileHeight / 2f);
            return new Vector3(worldX, worldY, 0f);
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float xRatio = worldPos.x / (TileWidth / 2f);
            float yRatio = worldPos.y / (TileHeight / 2f);

            int gridX = Mathf.RoundToInt((xRatio + yRatio) / 2f);
            int gridY = Mathf.RoundToInt((yRatio - xRatio) / 2f);

            return new Vector2Int(gridX, gridY);
        }

        public Vector3 WorldToScreen(Vector3 worldPos, Vector3 cameraPos, float zoom)
        {
            float screenX = (worldPos.x - cameraPos.x) * (100f / zoom) + (Screen.width / 2f);
            float screenY = (worldPos.y - cameraPos.y) * (100f / zoom) + (Screen.height / 2f);
            return new Vector3(screenX, screenY, 0f);
        }

        public Vector2Int ScreenToGrid(Vector3 screenPos, Vector3 cameraPos, float zoom)
        {
            float worldX = (screenPos.x - (Screen.width / 2f)) * (zoom / 100f) + cameraPos.x;
            float worldY = (screenPos.y - (Screen.height / 2f)) * (zoom / 100f) + cameraPos.y;
            return WorldToGrid(new Vector3(worldX, worldY, 0f));
        }
        #endregion
    }
}
