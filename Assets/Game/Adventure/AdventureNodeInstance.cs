using System;
using UnityEngine;
using Game.Data;

namespace Game.Adventure
{
    [Serializable]
    public class AdventureNodeInstance
    {
        public string NodeInstanceId;
        public string NodeDefId;
        public Vector2Int GridPosition;
        public NodeGatherState State = NodeGatherState.Hidden;
        public int CurrentQuantity;
        public int MaxQuantity;
        public long GatheringStartUtcTicks;
        public long RespawnStartUtcTicks;
        public bool IsCleared;

        public AdventureNodeInstance() { }

        public AdventureNodeInstance(string nodeInstanceId, string nodeDefId, Vector2Int gridPosition)
        {
            NodeInstanceId = nodeInstanceId;
            NodeDefId = nodeDefId;
            GridPosition = gridPosition;

            var def = ResourceNodeLibrary.GetDefinition(nodeDefId);
            if (def != null)
            {
                CurrentQuantity = def.YieldQuantity;
                MaxQuantity = def.MaxQuantity;
            }
            State = NodeGatherState.Hidden;
            IsCleared = false;
        }

        public void CheckAndUpdateState(long currentUtcTicks, ResourceNodeDefinition def)
        {
            if (def == null || IsCleared) return;

            if (State == NodeGatherState.Respawning)
            {
                if (def.RespawnDurationSeconds <= 0) return; // Non-respawning obstacle

                long elapsedTicks = currentUtcTicks - RespawnStartUtcTicks;
                double elapsedSeconds = TimeSpan.FromTicks(elapsedTicks).TotalSeconds;

                if (elapsedSeconds >= def.RespawnDurationSeconds)
                {
                    CurrentQuantity = def.MaxQuantity;
                    State = NodeGatherState.Available;
                    RespawnStartUtcTicks = 0;
                }
            }
        }
    }
}
