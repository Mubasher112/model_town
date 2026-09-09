using System;
using UnityEngine;

namespace Game.World
{
    public enum RotationAngle
    {
        Deg0 = 0,
        Deg90 = 90,
        Deg180 = 180,
        Deg270 = 270
    }

    [Serializable]
    public class ObjectFootprint
    {
        public int BaseWidth { get; set; } = 1;
        public int BaseHeight { get; set; } = 1;

        public ObjectFootprint() { }

        public ObjectFootprint(int baseWidth, int baseHeight)
        {
            BaseWidth = baseWidth;
            BaseHeight = baseHeight;
        }

        public (int width, int height) GetEffectiveSize(RotationAngle rotation)
        {
            if (rotation == RotationAngle.Deg90 || rotation == RotationAngle.Deg270)
            {
                return (BaseHeight, BaseWidth);
            }
            return (BaseWidth, BaseHeight);
        }
    }

    public enum PlacementResult
    {
        Valid,
        InvalidOutOfBounds,
        InvalidOccupied,
        InvalidWater,
        InvalidLocked,
        InvalidNotBuildable
    }

    public static class PlacementValidator
    {
        public static PlacementResult ValidatePlacement(WorldGrid grid, Vector2Int origin, ObjectFootprint footprint, RotationAngle rotation)
        {
            if (grid == null) return PlacementResult.InvalidOutOfBounds;

            var (width, height) = footprint.GetEffectiveSize(rotation);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int targetPos = new Vector2Int(origin.x + x, origin.y + y);

                    if (!grid.IsValidCoordinate(targetPos))
                    {
                        return PlacementResult.InvalidOutOfBounds;
                    }

                    TileData tile = grid.GetTile(targetPos);
                    if (tile == null)
                    {
                        return PlacementResult.InvalidOutOfBounds;
                    }

                    if (tile.Type == TileType.Water)
                    {
                        return PlacementResult.InvalidWater;
                    }

                    if (tile.IsLocked)
                    {
                        return PlacementResult.InvalidLocked;
                    }

                    if (tile.IsOccupied)
                    {
                        return PlacementResult.InvalidOccupied;
                    }

                    if (!tile.IsBuildable)
                    {
                        return PlacementResult.InvalidNotBuildable;
                    }
                }
            }

            return PlacementResult.Valid;
        }
    }
}
