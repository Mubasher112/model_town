using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Economy;
using Game.Player;

namespace Game.World
{
    public enum ExpansionOperationResult
    {
        Success,
        InvalidZone,
        AlreadyUnlocked,
        LevelRequirementNotMet,
        PopulationRequirementNotMet,
        CannotAffordCoins
    }

    public enum RoadConnectionType
    {
        Isolated,
        DeadEnd,
        Straight,
        Corner,
        TJunction,
        Cross
    }

    public interface IRoadAccessible
    {
        bool RequiresRoadAccess { get; }
        bool IsRoadAccessible { get; set; }
    }

    public interface ITransportNetwork
    {
        bool IsConnectedToNetwork(Vector2Int origin, ObjectFootprint footprint);
    }

    [Serializable]
    public class LandExpansionZone
    {
        public string ZoneId;
        public Vector2Int MinCoord;
        public Vector2Int MaxCoord;
        public int RequiredLevel;
        public int RequiredPopulation;
        public long CostCoins;
        public bool IsUnlocked;

        public LandExpansionZone() { }

        public LandExpansionZone(
            string zoneId,
            Vector2Int minCoord,
            Vector2Int maxCoord,
            int requiredLevel,
            long costCoins,
            bool isUnlocked = false,
            int requiredPopulation = 0)
        {
            ZoneId = zoneId;
            MinCoord = minCoord;
            MaxCoord = maxCoord;
            RequiredLevel = requiredLevel;
            CostCoins = costCoins;
            IsUnlocked = isUnlocked;
            RequiredPopulation = requiredPopulation;
        }
    }

    public class LandExpansionManager
    {
        private readonly WorldGrid _grid;
        private readonly Dictionary<string, LandExpansionZone> _zones = new Dictionary<string, LandExpansionZone>();

        public event Action<LandExpansionZone> OnZoneUnlocked;
        public event Action<string> OnNotificationMessage;

        public LandExpansionManager(WorldGrid grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            InitializeDefaultZones();
        }

        private void InitializeDefaultZones()
        {
            var startZone = new LandExpansionZone("zone_start", new Vector2Int(5, 5), new Vector2Int(24, 24), 1, 0, true);
            _zones[startZone.ZoneId] = startZone;

            var zoneNorth = new LandExpansionZone("expansion_01", new Vector2Int(5, 25), new Vector2Int(24, 29), 5, 500, false, 0);
            _zones[zoneNorth.ZoneId] = zoneNorth;

            var zoneEast = new LandExpansionZone("expansion_02", new Vector2Int(25, 5), new Vector2Int(29, 24), 8, 1000, false, 10);
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

        public ExpansionOperationResult TryPurchaseExpansion(
            string zoneId,
            PlayerProfile playerProfile,
            EconomyManager economyManager,
            int currentPopulation)
        {
            if (!_zones.TryGetValue(zoneId, out var zone))
            {
                return ExpansionOperationResult.InvalidZone;
            }

            if (zone.IsUnlocked)
            {
                return ExpansionOperationResult.AlreadyUnlocked;
            }

            if (playerProfile.Level < zone.RequiredLevel)
            {
                OnNotificationMessage?.Invoke($"Requires Level {zone.RequiredLevel}");
                return ExpansionOperationResult.LevelRequirementNotMet;
            }

            if (currentPopulation < zone.RequiredPopulation)
            {
                OnNotificationMessage?.Invoke($"Requires Population {zone.RequiredPopulation}");
                return ExpansionOperationResult.PopulationRequirementNotMet;
            }

            if (!economyManager.CanAffordCoins(zone.CostCoins))
            {
                OnNotificationMessage?.Invoke("Not enough coins");
                return ExpansionOperationResult.CannotAffordCoins;
            }

            // ATOMIC PURCHASE
            if (!economyManager.SpendCoins(zone.CostCoins))
            {
                return ExpansionOperationResult.CannotAffordCoins;
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
            OnNotificationMessage?.Invoke("Town Expanded! New land available.");
            return ExpansionOperationResult.Success;
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

        public LandExpansionZone GetZoneAt(Vector2Int coord)
        {
            foreach (var zone in _zones.Values)
            {
                if (coord.x >= zone.MinCoord.x && coord.x <= zone.MaxCoord.x &&
                    coord.y >= zone.MinCoord.y && coord.y <= zone.MaxCoord.y)
                {
                    return zone;
                }
            }
            return null;
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

    public class RoadManager : ITransportNetwork
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
            NotifyNeighbors(coord);
            return true;
        }

        public bool RemoveRoad(Vector2Int coord)
        {
            if (!_roadTiles.Contains(coord)) return false;

            _roadTiles.Remove(coord);
            _grid.SetTileType(coord, TileType.Grass);
            OnRoadChanged?.Invoke(coord, false);
            NotifyNeighbors(coord);
            return true;
        }

        private void NotifyNeighbors(Vector2Int coord)
        {
            Vector2Int[] dirs = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };
            foreach (var d in dirs)
            {
                Vector2Int neighbor = coord + d;
                if (IsRoad(neighbor))
                {
                    OnRoadChanged?.Invoke(neighbor, true);
                }
            }
        }

        public RoadConnectionType GetRoadConnectionType(Vector2Int coord)
        {
            if (!IsRoad(coord)) return RoadConnectionType.Isolated;

            bool n = IsRoad(coord + new Vector2Int(0, 1));
            bool e = IsRoad(coord + new Vector2Int(1, 0));
            bool s = IsRoad(coord + new Vector2Int(0, -1));
            bool w = IsRoad(coord + new Vector2Int(-1, 0));

            int count = (n ? 1 : 0) + (e ? 1 : 0) + (s ? 1 : 0) + (w ? 1 : 0);

            if (count == 0) return RoadConnectionType.Isolated;
            if (count == 1) return RoadConnectionType.DeadEnd;
            if (count == 4) return RoadConnectionType.Cross;
            if (count == 3) return RoadConnectionType.TJunction;

            if ((n && s) || (e && w)) return RoadConnectionType.Straight;
            return RoadConnectionType.Corner;
        }

        public bool IsConnectedToNetwork(Vector2Int origin, ObjectFootprint footprint)
        {
            if (footprint == null) return false;

            int width = footprint.BaseWidth;
            int height = footprint.BaseHeight;

            for (int x = -1; x <= width; x++)
            {
                for (int y = -1; y <= height; y++)
                {
                    if (x == -1 || x == width || y == -1 || y == height)
                    {
                        Vector2Int checkPos = new Vector2Int(origin.x + x, origin.y + y);
                        if (IsRoad(checkPos))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
}
