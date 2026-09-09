using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    public class PlacedObjectInstance
    {
        public string InstanceId { get; set; }
        public string ObjectTypeId { get; set; }
        public Vector2Int Origin { get; set; }
        public ObjectFootprint Footprint { get; set; }
        public RotationAngle Rotation { get; set; }

        public PlacedObjectInstance(string instanceId, string objectTypeId, Vector2Int origin, ObjectFootprint footprint, RotationAngle rotation = RotationAngle.Deg0)
        {
            InstanceId = instanceId;
            ObjectTypeId = objectTypeId;
            Origin = origin;
            Footprint = footprint;
            Rotation = rotation;
        }
    }

    public class ObjectPlacementManager
    {
        private readonly WorldGrid _grid;
        private readonly Dictionary<string, PlacedObjectInstance> _placedObjects = new Dictionary<string, PlacedObjectInstance>();

        public event Action<PlacedObjectInstance> OnObjectPlaced;
        public event Action<string> OnObjectRemoved;

        public ObjectPlacementManager(WorldGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public PlacementResult CheckPlacement(Vector2Int origin, ObjectFootprint footprint, RotationAngle rotation)
        {
            return PlacementValidator.ValidatePlacement(_grid, origin, footprint, rotation);
        }

        public bool TryPlaceObject(string instanceId, string objectTypeId, Vector2Int origin, ObjectFootprint footprint, RotationAngle rotation, out PlacementResult result)
        {
            result = CheckPlacement(origin, footprint, rotation);
            if (result != PlacementResult.Valid)
            {
                return false;
            }

            var instance = new PlacedObjectInstance(instanceId, objectTypeId, origin, footprint, rotation);
            var (width, height) = footprint.GetEffectiveSize(rotation);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(origin.x + x, origin.y + y);
                    TileData tile = _grid.GetTile(pos);
                    tile.IsOccupied = true;
                    tile.OccupyingObjectId = instanceId;
                }
            }

            _placedObjects[instanceId] = instance;
            OnObjectPlaced?.Invoke(instance);
            return true;
        }

        public bool RemoveObject(string instanceId)
        {
            if (!_placedObjects.TryGetValue(instanceId, out var instance))
            {
                return false;
            }

            var (width, height) = instance.Footprint.GetEffectiveSize(instance.Rotation);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(instance.Origin.x + x, instance.Origin.y + y);
                    TileData tile = _grid.GetTile(pos);
                    if (tile != null && tile.OccupyingObjectId == instanceId)
                    {
                        tile.IsOccupied = false;
                        tile.OccupyingObjectId = null;
                    }
                }
            }

            _placedObjects.Remove(instanceId);
            OnObjectRemoved?.Invoke(instanceId);
            return true;
        }

        public PlacedObjectInstance GetPlacedObject(string instanceId)
        {
            return _placedObjects.TryGetValue(instanceId, out var instance) ? instance : null;
        }

        public List<PlacedObjectInstance> GetAllPlacedObjects()
        {
            return new List<PlacedObjectInstance>(_placedObjects.Values);
        }

        public void Clear()
        {
            var keys = new List<string>(_placedObjects.Keys);
            foreach (var key in keys)
            {
                RemoveObject(key);
            }
        }
    }
}
