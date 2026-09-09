using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    public enum TileType
    {
        Grass,
        Water,
        Road,
        Field,
        Building,
        Obstacle
    }

    public class TileData
    {
        public Vector2Int Position { get; }
        public TileType Type { get; set; }
        public bool IsBuildable { get; set; }
        public bool IsOccupied { get; set; }
        public string OccupyingObjectId { get; set; }

        public TileData(Vector2Int position, TileType type = TileType.Grass, bool isBuildable = true)
        {
            Position = position;
            Type = type;
            IsBuildable = isBuildable;
            IsOccupied = false;
            OccupyingObjectId = null;
        }
    }

    public class WorldGrid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly TileData[,] _tiles;

        public event Action<Vector2Int, TileData> OnTileChanged;

        public WorldGrid(int width = 50, int height = 50)
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
                    bool border = (x == 0 || y == 0 || x == Width - 1 || y == Height - 1);
                    TileType type = border ? TileType.Water : TileType.Grass;
                    bool buildable = !border;

                    _tiles[x, y] = new TileData(new Vector2Int(x, y), type, buildable);
                }
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

        public bool CanPlaceObject(Vector2Int origin, int sizeX, int sizeY)
        {
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Vector2Int checkPos = new Vector2Int(origin.x + x, origin.y + y);
                    if (!IsValidCoordinate(checkPos)) return false;

                    TileData tile = GetTile(checkPos);
                    if (tile == null || !tile.IsBuildable || tile.IsOccupied)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool PlaceObject(Vector2Int origin, int sizeX, int sizeY, string objectId, TileType tileType = TileType.Building)
        {
            if (!CanPlaceObject(origin, sizeX, sizeY)) return false;

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                    TileData tile = GetTile(pos);
                    tile.IsOccupied = true;
                    tile.OccupyingObjectId = objectId;
                    tile.Type = tileType;
                    OnTileChanged?.Invoke(pos, tile);
                }
            }
            return true;
        }

        public bool RemoveObject(Vector2Int origin, int sizeX, int sizeY)
        {
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                    if (!IsValidCoordinate(pos)) continue;

                    TileData tile = GetTile(pos);
                    if (tile != null && tile.IsOccupied)
                    {
                        tile.IsOccupied = false;
                        tile.OccupyingObjectId = null;
                        tile.Type = TileType.Grass;
                        OnTileChanged?.Invoke(pos, tile);
                    }
                }
            }
            return true;
        }
    }
}
