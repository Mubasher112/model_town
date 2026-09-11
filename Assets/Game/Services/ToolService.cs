using System;
using System.Collections.Generic;
using Game.Data;

namespace Game.Services
{
    [Serializable]
    public class ToolInstance
    {
        public string ToolId;
        public int CurrentDurability;
        public int MaxDurability;

        public ToolInstance() { }

        public ToolInstance(string toolId, int durability, int maxDurability)
        {
            ToolId = toolId;
            CurrentDurability = durability;
            MaxDurability = maxDurability;
        }
    }

    public class ToolService
    {
        private readonly Dictionary<string, ToolInstance> _tools = new Dictionary<string, ToolInstance>();

        public ToolService(List<ToolInstance> savedTools = null)
        {
            if (savedTools != null && savedTools.Count > 0)
            {
                foreach (var tool in savedTools)
                {
                    _tools[tool.ToolId] = tool;
                }
            }

            EnsureDefaultTools();
        }

        public void EnsureDefaultTools()
        {
            var defs = ToolLibrary.GetAllTools();
            foreach (var def in defs)
            {
                if (!_tools.ContainsKey(def.ToolId))
                {
                    _tools[def.ToolId] = new ToolInstance(def.ToolId, def.MaxDurability, def.MaxDurability);
                }
            }
        }

        public ToolInstance GetTool(string toolId)
        {
            return _tools.TryGetValue(toolId, out var tool) ? tool : null;
        }

        public bool HasDurability(string toolId, int requiredDurability)
        {
            var tool = GetTool(toolId);
            return tool != null && tool.CurrentDurability >= requiredDurability;
        }

        public bool ConsumeDurability(string toolId, int amount)
        {
            if (!HasDurability(toolId, amount)) return false;

            var tool = GetTool(toolId);
            tool.CurrentDurability -= amount;
            return true;
        }

        public void RepairOrRefillTool(string toolId)
        {
            var tool = GetTool(toolId);
            if (tool != null)
            {
                tool.CurrentDurability = tool.MaxDurability;
            }
            else
            {
                var def = ToolLibrary.GetTool(toolId);
                if (def != null)
                {
                    _tools[toolId] = new ToolInstance(def.ToolId, def.MaxDurability, def.MaxDurability);
                }
            }
        }

        public void SetDurability(string toolId, int durability)
        {
            var tool = GetTool(toolId);
            if (tool != null)
            {
                tool.CurrentDurability = Math.Min(tool.MaxDurability, Math.Max(0, durability));
            }
        }

        public List<ToolInstance> ExportTools()
        {
            return new List<ToolInstance>(_tools.Values);
        }
    }
}
