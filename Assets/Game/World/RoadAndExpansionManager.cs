using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.World
{
    public class RoadManager
    {
        private readonly WorldGrid _grid;
        private readonly HashSet<Vector2Int> _roadTiles = new HashSet<Vector2Int>();

        public event Action<Vector2Int, bool> OnRoadChanged;

        public RoadManager(WorldGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        }

        public bool CanPlaceRoad(Vector2Int coord)
        {
            if (!_grid.IsValidCoordinate(coord)) return false;
            TileData tile = _grid.GetTile(coord);
            if (tile == null || tile.IsLocked || tile.IsOccupied || tile.Type == TileType.Water)
            {
                return false;
            }
            return true;
        }

        public bool PlaceRoad(Vector2Int coord)
        {
            if (!CanPlaceRoad(coord)) return false;

            _roadTiles.Add(coord);
            _grid.SetTileType(coord, TileType.Road);
            OnRoadChanged?.Invoke(coord, true);
            return true;
        }

        public bool RemoveRoad(Vector2Int coord)
        {
            if (!_roadTiles.Contains(coord)) return false;

            _roadTiles.Remove(coord);
            _grid.SetTileType(coord, TileType.Grass);
            OnRoadChanged?.Invoke(coord, false);
            return true;
        }

        public bool IsRoad(Vector2Int coord)
        {
            return _roadTiles.Contains(coord);
        }

        public List<Vector2Int> GetRoadTiles()
        {
            return new List<Vector2Int>(_roadTiles);
        }

        public void LoadRoads(IEnumerable<Vector2Int> roads)
        {
            _roadTiles.Clear();
            if (roads != null)
            {
                foreach (var coord in roads)
                {
                    _roadTiles.Add(coord);
                    _grid.SetTileType(coord, TileType.Road);
                }
            }
        }
    }

    [Serializable]
    public class LandExpansionZone
    {
        public string ZoneId { get; set; }
        public Vector2Int MinCoord { get; set; }
        public Vector2Int MaxCoord { get; set; }
        public int RequiredLevel { get; set; }
        public long CostCoins { get; set; }
        public bool IsUnlocked { get; set; }

        public LandExpansionZone() { }

        public LandExpansionZone(string zoneId, Vector2Int minCoord, Vector2Int maxCoord, int requiredLevel, long costCoins, bool isUnlocked = false)
        {
            ZoneId = zoneId;
            MinCoord = minCoord;
            MaxCoord = maxCoord;
            RequiredLevel = requiredLevel;
            CostCoins = costCoins;
            IsUnlocked = isUnlocked;
        }
    }

    public class LandExpansionManager
    {
        private readonly WorldGrid _grid;
        private readonly Dictionary<string, LandExpansionZone> _zones = new Dictionary<string, LandExpansionZone>();

        public event Action<LandExpansionZone> OnZoneUnlocked;

        public LandExpansionManager(WorldGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            InitializeDefaultZones();
        }

        private void InitializeDefaultZones()
        {
            // Initial unlocked area (zone_start: 5,5 to 24,24)
            var startZone = new LandExpansionZone("zone_start", new Vector2Int(5, 5), new Vector2Int(24, 24), 1, 0, true);
            _zones[startZone.ZoneId] = startZone;

            // Expansion 1 North (5,25 to 24,29)
            var zoneNorth = new LandExpansionZone("zone_north", new Vector2Int(5, 25), new Vector2Int(24, 29), 2, 200, false);
            _zones[zoneNorth.ZoneId] = zoneNorth;

            // Expansion 2 East (25,5 to 29,24)
            var zoneEast = new LandExpansionZone("zone_east", new Vector2Int(25, 5), new Vector2Int(29, 24), 3, 500, false);
            _zones[zoneEast.ZoneId] = zoneEast;

            ApplyZoneStates();
        }

        public void ApplyZoneStates()
        {
            foreach (var zone in _zones.Values)
            {
                for (int x = zone.MinCoord.x; x <= zone.MaxCoord.x; x++)
                {
                    for (int y = zone.MinCoord.y; y <= zone.MaxCoord.y; y++)
                    {
                        _grid.SetTileLocked(new Vector2Int(x, y), !zone.IsUnlocked);
                    }
                }
            }
        }

        public bool UnlockZone(string zoneId)
        {
            if (!_zones.TryGetValue(zoneId, out var zone) || zone.IsUnlocked)
            {
                return false;
            }

            zone.IsUnlocked = true;
            for (int x = zone.MinCoord.x; x <= zone.MaxCoord.x; x++)
            {
                for (int y = zone.MinCoord.y; y <= zone.MaxCoord.y; y++)
                {
                    _grid.SetTileLocked(new Vector2Int(x, y), false);
                }
            }

            OnZoneUnlocked?.Invoke(zone);
            return true;
        }

        public bool UnlockAllZonesDev()
        {
            foreach (var zone in _zones.Values)
            {
                if (!zone.IsUnlocked)
                {
                    UnlockZone(zone.ZoneId);
                }
            }
            return true;
        }

        public List<LandExpansionZone> GetAllZones()
        {
            return new List<LandExpansionZone>(_zones.Values);
        }

        public List<string> GetUnlockedZoneIds()
        {
            var list = new List<string>();
            foreach (var zone in _zones.Values)
            {
                if (zone.IsUnlocked) list.Add(zone.ZoneId);
            }
            return list;
        }

        public void LoadUnlockedZones(IEnumerable<string> unlockedZoneIds)
        {
            if (unlockedZoneIds == null) return;
            var set = new HashSet<string>(unlockedZoneIds);

            foreach (var zone in _zones.Values)
            {
                zone.IsUnlocked = set.Contains(zone.ZoneId);
            }

            ApplyZoneStates();
        }
    }
}
