using System;
using System.Collections.Generic;

namespace Game.Data
{
    public enum ToolType
    {
        Pickaxe,
        Axe,
        Shovel
    }

    [Serializable]
    public class ToolDefinition
    {
        public string ToolId;
        public string Name;
        public ToolType Type;
        public int MaxDurability;
        public int AllowedLevel = 1;

        public ToolDefinition() { }

        public ToolDefinition(string toolId, string name, ToolType type, int maxDurability, int allowedLevel = 1)
        {
            ToolId = toolId;
            Name = name;
            Type = type;
            MaxDurability = maxDurability;
            AllowedLevel = allowedLevel;
        }
    }

    public static class ToolLibrary
    {
        private static readonly Dictionary<string, ToolDefinition> _tools = new Dictionary<string, ToolDefinition>
        {
            { "tool_pickaxe", new ToolDefinition("tool_pickaxe", "Pickaxe", ToolType.Pickaxe, 30, 1) },
            { "tool_axe", new ToolDefinition("tool_axe", "Axe", ToolType.Axe, 30, 1) },
            { "tool_shovel", new ToolDefinition("tool_shovel", "Shovel", ToolType.Shovel, 25, 1) }
        };

        public static ToolDefinition GetTool(string toolId)
        {
            return _tools.TryGetValue(toolId, out var def) ? def : null;
        }

        public static List<ToolDefinition> GetAllTools()
        {
            return new List<ToolDefinition>(_tools.Values);
        }
    }
}
