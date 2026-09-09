using System;
using UnityEngine;
using Game.Data;
using Game.Services;

namespace Game.Farming
{
    public enum FieldState
    {
        Empty,
        Planted,
        Growing,
        Ready,
        Harvested
    }

    [Serializable]
    public class FieldInstance
    {
        public string FieldId;
        public Vector2Int GridPosition;
        public int Width = 1;
        public int Height = 1;

        public FieldState State = FieldState.Empty;
        public string CurrentCropId;
        public long PlantedUtcTicks;

        public FieldInstance() { }

        public FieldInstance(string fieldId, Vector2Int gridPosition, int width = 1, int height = 1)
        {
            FieldId = fieldId;
            GridPosition = gridPosition;
            Width = width;
            Height = height;
            State = FieldState.Empty;
            CurrentCropId = null;
            PlantedUtcTicks = 0;
        }

        public double GetElapsedGrowthSeconds(long currentUtcTicks)
        {
            if (State == FieldState.Empty || PlantedUtcTicks <= 0 || currentUtcTicks < PlantedUtcTicks)
            {
                return 0.0;
            }
            long diffTicks = currentUtcTicks - PlantedUtcTicks;
            return TimeSpan.FromTicks(diffTicks).TotalSeconds;
        }

        public float GetGrowthProgress(long currentUtcTicks, CropConfig cropConfig)
        {
            if (cropConfig == null || cropConfig.GrowthTimeSeconds <= 0f) return 0f;
            if (State == FieldState.Empty) return 0f;
            if (State == FieldState.Ready) return 1f;

            double elapsedSeconds = GetElapsedGrowthSeconds(currentUtcTicks);
            float progress = (float)(elapsedSeconds / cropConfig.GrowthTimeSeconds);
            return Mathf.Clamp(progress, 0f, 1f);
        }

        public bool CheckAndUpdateState(long currentUtcTicks, CropConfig cropConfig)
        {
            if (State == FieldState.Planted || State == FieldState.Growing)
            {
                if (cropConfig != null)
                {
                    double elapsed = GetElapsedGrowthSeconds(currentUtcTicks);
                    if (elapsed >= cropConfig.GrowthTimeSeconds)
                    {
                        State = FieldState.Ready;
                        return true;
                    }
                    else
                    {
                        State = FieldState.Growing;
                    }
                }
            }
            return false;
        }

        public int GetGrowthStage(long currentUtcTicks, CropConfig cropConfig)
        {
            if (State == FieldState.Empty) return 0; // Stage 0: Empty soil
            float progress = GetGrowthProgress(currentUtcTicks, cropConfig);

            if (progress >= 1.0f || State == FieldState.Ready) return 5; // Stage 5: Ready to harvest
            if (progress >= 0.75f) return 4;                             // Stage 4: Mature crop
            if (progress >= 0.5f) return 3;                              // Stage 3: Growing crop
            if (progress >= 0.25f) return 2;                             // Stage 2: Small plant
            return 1;                                                    // Stage 1: Recently planted
        }
    }
}
