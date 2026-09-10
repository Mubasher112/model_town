using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Player;
using Game.Economy;
using Game.Inventory;
using Game.Services;

namespace Game.Adventure
{
    public enum AdventureOperationResult
    {
        Success,
        LevelRequirementNotMet,
        CoinsRequirementNotMet,
        PopulationRequirementNotMet,
        AlreadyUnlocked,
        AreaLocked,
        InvalidPosition
    }

    public class AdventureManager
    {
        private readonly PlayerProfile _playerProfile;
        private readonly EconomyManager _economyManager;
        private readonly InventoryManager _inventoryManager;
        private readonly IGameTimeService _timeService;

        private readonly EnergyManager _energyManager;
        private readonly ToolService _toolService;
        private readonly ExplorationService _explorationService;
        private readonly ResourceGatheringService _gatheringService;

        private readonly Dictionary<string, AdventureNodeInstance> _nodes = new Dictionary<string, AdventureNodeInstance>();
        private readonly HashSet<string> _discoveredSpecialLocations = new HashSet<string>();

        private AdventureAreaDefinition _currentAreaDef;
        private bool _isUnlocked = false;
        private Vector2Int _playerPosition;
        private bool _isInAdventureMap = false;

        public bool IsUnlocked => _isUnlocked;
        public bool IsInAdventureMap => _isInAdventureMap;
        public Vector2Int PlayerPosition => _playerPosition;
        public AdventureAreaDefinition CurrentAreaDef => _currentAreaDef;

        public EnergyManager EnergyManager => _energyManager;
        public ToolService ToolService => _toolService;
        public ExplorationService ExplorationService => _explorationService;
        public ResourceGatheringService GatheringService => _gatheringService;

        public AdventureManager(
            PlayerProfile playerProfile,
            EconomyManager economyManager,
            InventoryManager inventoryManager,
            IGameTimeService timeService,
            EnergyState savedEnergyState = null,
            List<ToolInstance> savedTools = null,
            IEnumerable<Vector2Int> savedDiscoveredCells = null,
            bool isUnlocked = false)
        {
            _playerProfile = playerProfile;
            _economyManager = economyManager;
            _inventoryManager = inventoryManager;
            _timeService = timeService;
            _isUnlocked = isUnlocked;

            _currentAreaDef = AdventureAreaLibrary.GetArea("ancient_valley");
            _playerPosition = _currentAreaDef != null ? _currentAreaDef.EntryPosition : new Vector2Int(12, 2);

            _energyManager = new EnergyManager(savedEnergyState, timeService, 300f);
            _toolService = new ToolService(savedTools);
            _explorationService = new ExplorationService(savedDiscoveredCells);
            _gatheringService = new ResourceGatheringService(inventoryManager, economyManager, playerProfile, _toolService, _energyManager, timeService);

            InitializeDefaultAreaNodes();
            // Initial discovery around entry point
            _explorationService.DiscoverRadius(_playerPosition, 3);
            UpdateNodeVisibility();
        }

        private void InitializeDefaultAreaNodes()
        {
            if (_nodes.Count > 0) return;

            // Pre-populate Ancient Valley map nodes (25x25)
            AddNode("node_stone_1", "stone_deposit", new Vector2Int(10, 5));
            AddNode("node_stone_2", "stone_deposit", new Vector2Int(14, 8));
            AddNode("node_tree_1", "tree_node", new Vector2Int(8, 4));
            AddNode("node_tree_2", "tree_node", new Vector2Int(16, 6));
            AddNode("node_clay_1", "clay_pit", new Vector2Int(6, 12));
            AddNode("node_ore_1", "ore_vein", new Vector2Int(18, 15));
            AddNode("node_crystal_1", "crystal_geode", new Vector2Int(20, 20));

            // Obstacles blocking pathways
            AddNode("node_rock_obstacle_1", "large_boulder_obstacle", new Vector2Int(12, 10));
            AddNode("node_tree_obstacle_1", "fallen_tree_obstacle", new Vector2Int(15, 12));
        }

        private void AddNode(string instanceId, string defId, Vector2Int pos)
        {
            var node = new AdventureNodeInstance(instanceId, defId, pos);
            _nodes[instanceId] = node;
        }

        public AdventureOperationResult TryUnlockAdventure(int currentPopulation)
        {
            if (_isUnlocked) return AdventureOperationResult.AlreadyUnlocked;
            if (_currentAreaDef == null) return AdventureOperationResult.AreaLocked;

            if (_playerProfile.Level < _currentAreaDef.RequiredLevel)
            {
                return AdventureOperationResult.LevelRequirementNotMet;
            }

            if (currentPopulation < _currentAreaDef.RequiredPopulation)
            {
                return AdventureOperationResult.PopulationRequirementNotMet;
            }

            if (!_economyManager.CanAffordCoins(_currentAreaDef.RequiredCoins))
            {
                return AdventureOperationResult.CoinsRequirementNotMet;
            }

            _economyManager.SpendCoins(_currentAreaDef.RequiredCoins);
            _isUnlocked = true;
            return AdventureOperationResult.Success;
        }

        public bool EnterAdventureArea()
        {
            if (!_isUnlocked) return false;
            _isInAdventureMap = true;
            _playerPosition = _currentAreaDef.EntryPosition;
            _explorationService.DiscoverRadius(_playerPosition, 3);
            UpdateNodeVisibility();
            return true;
        }

        public bool ExitAdventureArea()
        {
            _isInAdventureMap = false;
            return true;
        }

        public bool MovePlayer(Vector2Int targetPosition)
        {
            if (!_isInAdventureMap) return false;

            // Validate boundaries
            if (targetPosition.x < 0 || targetPosition.x >= _currentAreaDef.Width ||
                targetPosition.y < 0 || targetPosition.y >= _currentAreaDef.Height)
            {
                return false;
            }

            // Check obstacle collision
            foreach (var node in _nodes.Values)
            {
                if (node.GridPosition == targetPosition && !node.IsCleared)
                {
                    var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
                    if (def != null && def.Type == AdventureNodeType.Obstacle)
                    {
                        return false; // Path blocked by uncleared obstacle
                    }
                }
            }

            _playerPosition = targetPosition;
            _explorationService.DiscoverRadius(_playerPosition, 3);
            UpdateNodeVisibility();
            CheckSpecialLocations();
            return true;
        }

        public void UpdateNodeVisibility()
        {
            long now = _timeService.CurrentUtcTicks;
            foreach (var node in _nodes.Values)
            {
                var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
                node.CheckAndUpdateState(now, def);

                if (_explorationService.IsDiscovered(node.GridPosition))
                {
                    if (node.State == NodeGatherState.Hidden)
                    {
                        node.State = NodeGatherState.Available;
                    }
                }
            }
        }

        private void CheckSpecialLocations()
        {
            // Ancient Ruins check at (20, 20)
            if (_playerPosition == new Vector2Int(20, 20) && !_discoveredSpecialLocations.Contains("loc_ancient_ruins"))
            {
                DiscoverSpecialLocation("loc_ancient_ruins");
            }
            // Abandoned Mine at (18, 15)
            if (_playerPosition == new Vector2Int(18, 15) && !_discoveredSpecialLocations.Contains("loc_abandoned_mine"))
            {
                DiscoverSpecialLocation("loc_abandoned_mine");
            }
            // Hidden Grove at (6, 12)
            if (_playerPosition == new Vector2Int(6, 12) && !_discoveredSpecialLocations.Contains("loc_hidden_grove"))
            {
                DiscoverSpecialLocation("loc_hidden_grove");
            }
        }

        public bool DiscoverSpecialLocation(string locationId)
        {
            if (_discoveredSpecialLocations.Contains(locationId)) return false;

            var loc = SpecialLocationLibrary.GetLocation(locationId);
            if (loc == null) return false;

            _discoveredSpecialLocations.Add(locationId);
            _playerProfile.AddXP(loc.DiscoveryXpReward);
            _economyManager.EarnCoins(loc.CoinReward);

            if (!string.IsNullOrEmpty(loc.BonusItemId) && loc.BonusItemQuantity > 0)
            {
                _inventoryManager.AddItem(loc.BonusItemId, loc.Name + " Bonus", ItemType.RawMaterial, loc.BonusItemQuantity);
            }

            return true;
        }

        public GatheringOperationResult GatherNodeAtPosition(Vector2Int pos, out int yieldAmount, out int xpEarned)
        {
            yieldAmount = 0;
            xpEarned = 0;

            AdventureNodeInstance targetNode = null;
            foreach (var node in _nodes.Values)
            {
                if (node.GridPosition == pos)
                {
                    targetNode = node;
                    break;
                }
            }

            if (targetNode == null) return GatheringOperationResult.NodeNotFound;

            var result = _gatheringService.GatherNode(targetNode, out yieldAmount, out xpEarned);
            UpdateNodeVisibility();
            return result;
        }

        public void LoadNodes(List<AdventureNodeInstance> savedNodes)
        {
            if (savedNodes == null || savedNodes.Count == 0) return;

            _nodes.Clear();
            foreach (var node in savedNodes)
            {
                _nodes[node.NodeInstanceId] = node;
            }
        }

        public List<AdventureNodeInstance> ExportNodes()
        {
            return new List<AdventureNodeInstance>(_nodes.Values);
        }

        public List<string> ExportDiscoveredLocations()
        {
            return new List<string>(_discoveredSpecialLocations);
        }

        public void LoadDiscoveredLocations(List<string> locations)
        {
            if (locations == null) return;
            _discoveredSpecialLocations.Clear();
            foreach (var loc in locations)
            {
                _discoveredSpecialLocations.Add(loc);
            }
        }

        public void DevUnlockAdventure() => _isUnlocked = true;
        public void DevSetPlayerPos(Vector2Int pos) => _playerPosition = pos;
        public void DevRevealAllMap() => _explorationService.DiscoverRadius(new Vector2Int(12, 12), 20);
        public void DevClearAllObstacles()
        {
            foreach (var node in _nodes.Values)
            {
                var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
                if (def != null && def.Type == AdventureNodeType.Obstacle)
                {
                    node.IsCleared = true;
                    node.State = NodeGatherState.Depleted;
                }
            }
        }
    }
}
