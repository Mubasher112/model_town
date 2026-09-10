using System;
using UnityEngine;
using Game.Data;
using Game.Inventory;
using Game.Economy;
using Game.Player;
using Game.Services;

namespace Game.Adventure
{
    public enum GatheringOperationResult
    {
        Success,
        NodeNotFound,
        NodeNotAvailable,
        MissingTool,
        ToolDurabilityInsufficient,
        EnergyInsufficient,
        StorageFull
    }

    public class ResourceGatheringService
    {
        private readonly InventoryManager _inventoryManager;
        private readonly EconomyManager _economyManager;
        private readonly PlayerProfile _playerProfile;
        private readonly ToolService _toolService;
        private readonly EnergyManager _energyManager;
        private readonly IGameTimeService _timeService;

        public ResourceGatheringService(
            InventoryManager inventoryManager,
            EconomyManager economyManager,
            PlayerProfile playerProfile,
            ToolService toolService,
            EnergyManager energyManager,
            IGameTimeService timeService)
        {
            _inventoryManager = inventoryManager;
            _economyManager = economyManager;
            _playerProfile = playerProfile;
            _toolService = toolService;
            _energyManager = energyManager;
            _timeService = timeService;
        }

        public GatheringOperationResult CanGatherNode(AdventureNodeInstance node)
        {
            if (node == null || node.IsCleared) return GatheringOperationResult.NodeNotFound;

            long now = _timeService.CurrentUtcTicks;
            var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
            if (def == null) return GatheringOperationResult.NodeNotFound;

            node.CheckAndUpdateState(now, def);

            if (node.State != NodeGatherState.Available && node.State != NodeGatherState.Discovered)
            {
                return GatheringOperationResult.NodeNotAvailable;
            }

            // Check Tool Requirement
            if (!string.IsNullOrEmpty(def.RequiredToolId))
            {
                var tool = _toolService.GetTool(def.RequiredToolId);
                if (tool == null) return GatheringOperationResult.MissingTool;

                if (!_toolService.HasDurability(def.RequiredToolId, def.RequiredToolDurability))
                {
                    return GatheringOperationResult.ToolDurabilityInsufficient;
                }
            }

            // Check Energy Requirement
            if (!_energyManager.CanConsumeEnergy(def.EnergyCost))
            {
                return GatheringOperationResult.EnergyInsufficient;
            }

            // Check Inventory Capacity for items
            if (!string.IsNullOrEmpty(def.OutputItemId) && def.YieldQuantity > 0)
            {
                if (!_inventoryManager.CanAddItem(1)) // At least 1 space for item yield
                {
                    return GatheringOperationResult.StorageFull;
                }
            }

            return GatheringOperationResult.Success;
        }

        public GatheringOperationResult GatherNode(AdventureNodeInstance node, out int yieldAmount, out int xpEarned)
        {
            yieldAmount = 0;
            xpEarned = 0;

            var checkResult = CanGatherNode(node);
            if (checkResult != GatheringOperationResult.Success)
            {
                return checkResult;
            }

            var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
            long now = _timeService.CurrentUtcTicks;

            // Deduct Energy and Tool Durability safely
            _energyManager.ConsumeEnergy(def.EnergyCost);
            if (!string.IsNullOrEmpty(def.RequiredToolId))
            {
                _toolService.ConsumeDurability(def.RequiredToolId, def.RequiredToolDurability);
            }

            // Calculate yield and award items
            yieldAmount = Math.Min(node.CurrentQuantity, def.YieldQuantity);
            if (!string.IsNullOrEmpty(def.OutputItemId) && yieldAmount > 0)
            {
                string itemName = FormatItemName(def.OutputItemId);
                _inventoryManager.AddItem(def.OutputItemId, itemName, GetItemTypeForResource(def.OutputItemId), yieldAmount);
            }

            // Award XP & Coins
            xpEarned = def.XpReward;
            _playerProfile.AddXP(xpEarned);
            if (def.CoinsReward > 0)
            {
                _economyManager.EarnCoins(def.CoinsReward);
            }

            // Update Node State
            node.CurrentQuantity -= yieldAmount;
            if (node.CurrentQuantity <= 0)
            {
                if (def.Type == AdventureNodeType.Obstacle)
                {
                    node.IsCleared = true;
                    node.State = NodeGatherState.Depleted;
                }
                else if (def.RespawnDurationSeconds > 0)
                {
                    node.State = NodeGatherState.Respawning;
                    node.RespawnStartUtcTicks = now;
                }
                else
                {
                    node.State = NodeGatherState.Depleted;
                }
            }

            return GatheringOperationResult.Success;
        }

        private string FormatItemName(string itemId)
        {
            switch (itemId)
            {
                case "item_stone": return "Stone";
                case "item_wood": return "Wood";
                case "item_clay": return "Clay";
                case "item_ore": return "Iron Ore";
                case "item_rare_crystal": return "Rare Crystal";
                default: return "Resource";
            }
        }

        private ItemType GetItemTypeForResource(string itemId)
        {
            return ItemType.RawMaterial;
        }
    }
}
