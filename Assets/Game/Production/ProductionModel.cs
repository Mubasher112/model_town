using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Production
{
    public enum ProductionJobState
    {
        Queued,
        Producing,
        Ready,
        Blocked
    }

    [Serializable]
    public class ProductionJob
    {
        public string JobId;
        public string RecipeId;
        public long StartUtcTicks;
        public ProductionJobState State = ProductionJobState.Queued;
        public bool XpAwarded = false;

        public ProductionJob() { }

        public ProductionJob(string jobId, string recipeId)
        {
            JobId = jobId;
            RecipeId = recipeId;
            StartUtcTicks = 0;
            State = ProductionJobState.Queued;
            XpAwarded = false;
        }

        public double GetElapsedSeconds(long currentUtcTicks)
        {
            if (StartUtcTicks <= 0 || currentUtcTicks < StartUtcTicks) return 0.0;
            return TimeSpan.FromTicks(currentUtcTicks - StartUtcTicks).TotalSeconds;
        }

        public float GetProgress(long currentUtcTicks, RecipeConfig config)
        {
            if (config == null || config.ProductionTimeSeconds <= 0f) return 1f;
            if (State == ProductionJobState.Ready) return 1f;
            if (State == ProductionJobState.Queued) return 0f;

            double elapsed = GetElapsedSeconds(currentUtcTicks);
            float progress = (float)(elapsed / config.ProductionTimeSeconds);
            return Mathf.Clamp(progress, 0f, 1f);
        }

        public bool CheckAndUpdateState(long currentUtcTicks, RecipeConfig config)
        {
            if (config == null) return false;

            if (State == ProductionJobState.Producing)
            {
                if (config.ProductionTimeSeconds <= 0f || GetElapsedSeconds(currentUtcTicks) >= config.ProductionTimeSeconds)
                {
                    State = ProductionJobState.Ready;
                    return true;
                }
            }
            return false;
        }
    }

    [Serializable]
    public class ProductionBuildingInstance
    {
        public string BuildingInstanceId;
        public string BuildingId;
        public int QueueCapacity = 2;
        public List<ProductionJob> JobsQueue = new List<ProductionJob>();

        public ProductionBuildingInstance() { }

        public ProductionBuildingInstance(string buildingInstanceId, string buildingId, int queueCapacity = 2)
        {
            BuildingInstanceId = buildingInstanceId;
            BuildingId = buildingId;
            QueueCapacity = queueCapacity;
            JobsQueue = new List<ProductionJob>();
        }

        public ProductionJob CurrentActiveJob
        {
            get
            {
                if (JobsQueue != null && JobsQueue.Count > 0)
                {
                    return JobsQueue[0];
                }
                return null;
            }
        }

        public bool IsQueueFull => JobsQueue != null && JobsQueue.Count >= QueueCapacity;
    }
}
