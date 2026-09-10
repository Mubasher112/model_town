using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    public enum AdventureNodeType
    {
        Resource,
        Obstacle,
        SpecialLocation,
        ExitPoint
    }

    public enum NodeGatherState
    {
        Hidden,
        Discovered,
        Available,
        Gathering,
        Depleted,
        Respawning
    }

    [Serializable]
    public class ResourceNodeDefinition
    {
        public string NodeDefId;
        public string Name;
        public AdventureNodeType Type = AdventureNodeType.Resource;
        public string OutputItemId;
        public string RequiredToolId;
        public int RequiredToolDurability = 1;
        public int EnergyCost = 2;
        public int YieldQuantity = 5;
        public int MaxQuantity = 5;
        public float GatheringDurationSeconds = 3f;
        public int XpReward = 5;
        public int CoinsReward = 0;
        public float RespawnDurationSeconds = 1800f; // 30 minutes

        public ResourceNodeDefinition() { }

        public ResourceNodeDefinition(
            string nodeDefId,
            string name,
            AdventureNodeType type,
            string outputItemId,
            string requiredToolId,
            int requiredToolDurability,
            int energyCost,
            int yieldQuantity,
            float gatheringDurationSeconds,
            int xpReward,
            float respawnDurationSeconds = 1800f)
        {
            NodeDefId = nodeDefId;
            Name = name;
            Type = type;
            OutputItemId = outputItemId;
            RequiredToolId = requiredToolId;
            RequiredToolDurability = requiredToolDurability;
            EnergyCost = energyCost;
            YieldQuantity = yieldQuantity;
            MaxQuantity = yieldQuantity;
            GatheringDurationSeconds = gatheringDurationSeconds;
            XpReward = xpReward;
            RespawnDurationSeconds = respawnDurationSeconds;
        }
    }

    public static class ResourceNodeLibrary
    {
        private static readonly Dictionary<string, ResourceNodeDefinition> _nodeDefs = new Dictionary<string, ResourceNodeDefinition>
        {
            {
                "stone_deposit",
                new ResourceNodeDefinition("stone_deposit", "Stone Deposit", AdventureNodeType.Resource, "item_stone", "tool_pickaxe", 1, 2, 5, 2.5f, 5, 1800f)
            },
            {
                "tree_node",
                new ResourceNodeDefinition("tree_node", "Pine Tree", AdventureNodeType.Resource, "item_wood", "tool_axe", 1, 1, 3, 2.0f, 3, 1200f)
            },
            {
                "clay_pit",
                new ResourceNodeDefinition("clay_pit", "Clay Deposit", AdventureNodeType.Resource, "item_clay", "tool_shovel", 1, 2, 4, 3.0f, 6, 2400f)
            },
            {
                "ore_vein",
                new ResourceNodeDefinition("ore_vein", "Iron Ore Vein", AdventureNodeType.Resource, "item_ore", "tool_pickaxe", 2, 3, 3, 4.0f, 10, 3600f)
            },
            {
                "crystal_geode",
                new ResourceNodeDefinition("crystal_geode", "Crystal Geode", AdventureNodeType.Resource, "item_rare_crystal", "tool_pickaxe", 3, 4, 1, 5.0f, 20, 7200f)
            },
            {
                "large_boulder_obstacle",
                new ResourceNodeDefinition("large_boulder_obstacle", "Large Boulder", AdventureNodeType.Obstacle, "item_stone", "tool_pickaxe", 5, 5, 15, 6.0f, 25, -1f) // -1 = no respawn
            },
            {
                "fallen_tree_obstacle",
                new ResourceNodeDefinition("fallen_tree_obstacle", "Fallen Log", AdventureNodeType.Obstacle, "item_wood", "tool_axe", 4, 4, 12, 5.0f, 20, -1f)
            }
        };

        public static ResourceNodeDefinition GetDefinition(string nodeDefId)
        {
            return _nodeDefs.TryGetValue(nodeDefId, out var def) ? def : null;
        }

        public static List<ResourceNodeDefinition> GetAllDefinitions()
        {
            return new List<ResourceNodeDefinition>(_nodeDefs.Values);
        }
    }

    [Serializable]
    public class SpecialLocationDefinition
    {
        public string LocationId;
        public string Name;
        public string Description;
        public int DiscoveryXpReward = 50;
        public int CoinReward = 100;
        public string BonusItemId = "item_rare_crystal";
        public int BonusItemQuantity = 2;

        public SpecialLocationDefinition() { }

        public SpecialLocationDefinition(string locationId, string name, string description, int discoveryXpReward, int coinReward, string bonusItemId, int bonusItemQuantity)
        {
            LocationId = locationId;
            Name = name;
            Description = description;
            DiscoveryXpReward = discoveryXpReward;
            CoinReward = coinReward;
            BonusItemId = bonusItemId;
            BonusItemQuantity = bonusItemQuantity;
        }
    }

    public static class SpecialLocationLibrary
    {
        private static readonly Dictionary<string, SpecialLocationDefinition> _locations = new Dictionary<string, SpecialLocationDefinition>
        {
            {
                "loc_ancient_ruins",
                new SpecialLocationDefinition("loc_ancient_ruins", "Ancient Ruins", "Mysterious ruins containing rare ancient crystals.", 75, 150, "item_rare_crystal", 2)
            },
            {
                "loc_abandoned_mine",
                new SpecialLocationDefinition("loc_abandoned_mine", "Abandoned Shaft", "An old mining shaft rich in raw ore and leftover coins.", 50, 100, "item_ore", 5)
            },
            {
                "loc_hidden_grove",
                new SpecialLocationDefinition("loc_hidden_grove", "Hidden Grove", "A serene undisturbed grove with abundant high-grade timber.", 50, 80, "item_wood", 10)
            }
        };

        public static SpecialLocationDefinition GetLocation(string locationId)
        {
            return _locations.TryGetValue(locationId, out var def) ? def : null;
        }
    }

    [Serializable]
    public class AdventureAreaDefinition
    {
        public string AreaId;
        public string Name;
        public string Description;
        public int Width = 25;
        public int Height = 25;
        public int RequiredLevel = 8;
        public long RequiredCoins = 1000;
        public int RequiredPopulation = 10;
        public Vector2Int EntryPosition = new Vector2Int(12, 2);

        public AdventureAreaDefinition() { }

        public AdventureAreaDefinition(string areaId, string name, string description, int width, int height, int requiredLevel, long requiredCoins, int requiredPopulation, Vector2Int entryPosition)
        {
            AreaId = areaId;
            Name = name;
            Description = description;
            Width = width;
            Height = height;
            RequiredLevel = requiredLevel;
            RequiredCoins = requiredCoins;
            RequiredPopulation = requiredPopulation;
            EntryPosition = entryPosition;
        }
    }

    public static class AdventureAreaLibrary
    {
        private static readonly Dictionary<string, AdventureAreaDefinition> _areas = new Dictionary<string, AdventureAreaDefinition>
        {
            {
                "ancient_valley",
                new AdventureAreaDefinition("ancient_valley", "Ancient Valley", "A lush unexplored valley rich in minerals, ancient trees, and hidden ruins.", 25, 25, 8, 1000, 10, new Vector2Int(12, 2))
            }
        };

        public static AdventureAreaDefinition GetArea(string areaId)
        {
            return _areas.TryGetValue(areaId, out var def) ? def : null;
        }
    }
}
