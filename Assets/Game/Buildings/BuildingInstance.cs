using System;
using UnityEngine;
using Game.Data;
using Game.World;

namespace Game.Buildings
{
    public enum BuildingState
    {
        Preview,
        UnderConstruction,
        Completed,
        Upgrading,
        Locked
    }

    [Serializable]
    public class BuildingInstance
    {
        public string InstanceId;
        public string BuildingId;
        public Vector2Int GridPosition;
        public RotationAngle Rotation = RotationAngle.Deg0;
        public int Level = 1;

        public BuildingState State = BuildingState.UnderConstruction;
        public long ConstructionStartUtcTicks;
        public long UpgradeStartUtcTicks;
        public bool CompletionXpAwarded = false;

        public BuildingInstance() { }

        public BuildingInstance(string instanceId, string buildingId, Vector2Int gridPosition, RotationAngle rotation = RotationAngle.Deg0)
        {
            InstanceId = instanceId;
            BuildingId = buildingId;
            GridPosition = gridPosition;
            Rotation = rotation;
            Level = 1;
            State = BuildingState.UnderConstruction;
            ConstructionStartUtcTicks = 0;
            UpgradeStartUtcTicks = 0;
            CompletionXpAwarded = false;
        }

        public double GetElapsedConstructionSeconds(long currentUtcTicks)
        {
            if (ConstructionStartUtcTicks <= 0 || currentUtcTicks < ConstructionStartUtcTicks) return 0.0;
            return TimeSpan.FromTicks(currentUtcTicks - ConstructionStartUtcTicks).TotalSeconds;
        }

        public double GetElapsedUpgradeSeconds(long currentUtcTicks)
        {
            if (UpgradeStartUtcTicks <= 0 || currentUtcTicks < UpgradeStartUtcTicks) return 0.0;
            return TimeSpan.FromTicks(currentUtcTicks - UpgradeStartUtcTicks).TotalSeconds;
        }

        public float GetConstructionProgress(long currentUtcTicks, BuildingConfig config)
        {
            if (config == null || config.ConstructionTimeSeconds <= 0f) return 1f;
            if (State == BuildingState.Completed) return 1f;
            if (State != BuildingState.UnderConstruction) return 0f;

            double elapsed = GetElapsedConstructionSeconds(currentUtcTicks);
            float progress = (float)(elapsed / config.ConstructionTimeSeconds);
            return Mathf.Clamp(progress, 0f, 1f);
        }

        public float GetUpgradeProgress(long currentUtcTicks, BuildingConfig config)
        {
            if (config == null || config.UpgradeTimeSeconds <= 0f) return 1f;
            if (State != BuildingState.Upgrading) return 0f;

            double elapsed = GetElapsedUpgradeSeconds(currentUtcTicks);
            float progress = (float)(elapsed / config.UpgradeTimeSeconds);
            return Mathf.Clamp(progress, 0f, 1f);
        }

        public bool CheckAndUpdateState(long currentUtcTicks, BuildingConfig config)
        {
            if (config == null) return false;

            if (State == BuildingState.UnderConstruction)
            {
                if (config.ConstructionTimeSeconds <= 0f || GetElapsedConstructionSeconds(currentUtcTicks) >= config.ConstructionTimeSeconds)
                {
                    State = BuildingState.Completed;
                    return true;
                }
            }
            else if (State == BuildingState.Upgrading)
            {
                if (config.UpgradeTimeSeconds <= 0f || GetElapsedUpgradeSeconds(currentUtcTicks) >= config.UpgradeTimeSeconds)
                {
                    State = BuildingState.Completed;
                    Level++;
                    return true;
                }
            }

            return false;
        }
    }
}
