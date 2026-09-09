using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Inventory;
using Game.Player;
using Game.Services;

namespace Game.Production
{
    public enum ProductionOperationResult
    {
        Success,
        InvalidBuilding,
        BuildingNotCompleted,
        RecipeLocked,
        QueueFull,
        MissingIngredients,
        StorageFull,
        NoJobToCollect,
        JobNotReady,
        InvalidRecipe
    }

    public class ProductionManager
    {
        private readonly Dictionary<string, ProductionBuildingInstance> _productionBuildings = new Dictionary<string, ProductionBuildingInstance>();
        private readonly InventoryManager _inventoryManager;
        private readonly PlayerProfile _playerProfile;
        private readonly StandardGameTimeService _timeService;

        public event Action<ProductionBuildingInstance> OnProductionStateChanged;
        public event Action<string> OnNotificationMessage;

        public ProductionManager(
            InventoryManager inventoryManager,
            PlayerProfile playerProfile,
            StandardGameTimeService timeService)
        {
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        public void RegisterProductionBuilding(ProductionBuildingInstance building)
        {
            if (building == null) return;
            _productionBuildings[building.BuildingInstanceId] = building;
            UpdateSingleBuildingProduction(building);
        }

        public ProductionBuildingInstance GetProductionBuilding(string buildingInstanceId)
        {
            return _productionBuildings.TryGetValue(buildingInstanceId, out var b) ? b : null;
        }

        public List<ProductionBuildingInstance> GetAllProductionBuildings()
        {
            return new List<ProductionBuildingInstance>(_productionBuildings.Values);
        }

        public void UpdateSingleBuildingProduction(ProductionBuildingInstance b)
        {
            if (b == null || b.JobsQueue == null || b.JobsQueue.Count == 0) return;

            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            bool stateChanged = false;

            for (int i = 0; i < b.JobsQueue.Count; i++)
            {
                var job = b.JobsQueue[i];
                var recipe = RecipeLibrary.GetRecipe(job.RecipeId);
                if (recipe == null) continue;

                if (i == 0)
                {
                    // Active head job
                    if (job.State == ProductionJobState.Queued)
                    {
                        // Check and consume ingredients when job starts producing
                        if (!HasRequiredIngredients(recipe))
                        {
                            job.State = ProductionJobState.Blocked;
                            stateChanged = true;
                            break;
                        }

                        ConsumeIngredients(recipe);
                        job.State = ProductionJobState.Producing;
                        job.StartUtcTicks = nowTicks;
                        stateChanged = true;
                    }
                    else if (job.State == ProductionJobState.Blocked)
                    {
                        // Retry starting blocked job
                        if (HasRequiredIngredients(recipe))
                        {
                            ConsumeIngredients(recipe);
                            job.State = ProductionJobState.Producing;
                            job.StartUtcTicks = nowTicks;
                            stateChanged = true;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (job.State == ProductionJobState.Producing)
                    {
                        if (job.CheckAndUpdateState(nowTicks, recipe))
                        {
                            stateChanged = true;
                        }
                    }

                    if (job.State != ProductionJobState.Ready)
                    {
                        // Current job is still producing/blocked, queued jobs wait
                        break;
                    }
                }
            }

            if (stateChanged)
            {
                OnProductionStateChanged?.Invoke(b);
            }
        }

        public void UpdateAllProductionBuildings()
        {
            foreach (var b in _productionBuildings.Values)
            {
                UpdateSingleBuildingProduction(b);
            }
        }

        public bool HasRequiredIngredients(RecipeConfig recipe)
        {
            if (recipe == null || recipe.Ingredients == null) return true;
            foreach (var ing in recipe.Ingredients)
            {
                if (_inventoryManager.GetQuantity(ing.ItemId) < ing.Quantity)
                {
                    return false;
                }
            }
            return true;
        }

        private void ConsumeIngredients(RecipeConfig recipe)
        {
            if (recipe == null || recipe.Ingredients == null) return;
            foreach (var ing in recipe.Ingredients)
            {
                _inventoryManager.RemoveItem(ing.ItemId, ing.Quantity);
            }
        }

        public ProductionOperationResult StartProductionJob(string buildingInstanceId, string recipeId)
        {
            var b = GetProductionBuilding(buildingInstanceId);
            if (b == null) return ProductionOperationResult.InvalidBuilding;

            var recipe = RecipeLibrary.GetRecipe(recipeId);
            if (recipe == null) return ProductionOperationResult.InvalidRecipe;

            if (_playerProfile.Level < recipe.UnlockLevel)
            {
                OnNotificationMessage?.Invoke($"Recipe requires Level {recipe.UnlockLevel}");
                return ProductionOperationResult.RecipeLocked;
            }

            if (b.IsQueueFull)
            {
                OnNotificationMessage?.Invoke("Production queue is full");
                return ProductionOperationResult.QueueFull;
            }

            // Check required ingredients before queuing
            if (!HasRequiredIngredients(recipe))
            {
                OnNotificationMessage?.Invoke("Insufficient ingredients");
                return ProductionOperationResult.MissingIngredients;
            }

            string jobId = "job_" + Guid.NewGuid().ToString().Substring(0, 6);
            var newJob = new ProductionJob(jobId, recipeId);

            b.JobsQueue.Add(newJob);

            UpdateSingleBuildingProduction(b);

            OnNotificationMessage?.Invoke($"{recipe.Name} added to production!");
            return ProductionOperationResult.Success;
        }

        public ProductionOperationResult CollectProduct(string buildingInstanceId)
        {
            var b = GetProductionBuilding(buildingInstanceId);
            if (b == null) return ProductionOperationResult.InvalidBuilding;

            if (b.JobsQueue == null || b.JobsQueue.Count == 0)
            {
                return ProductionOperationResult.NoJobToCollect;
            }

            UpdateSingleBuildingProduction(b);
            var headJob = b.JobsQueue[0];

            if (headJob.State != ProductionJobState.Ready)
            {
                return ProductionOperationResult.JobNotReady;
            }

            var recipe = RecipeLibrary.GetRecipe(headJob.RecipeId);
            if (recipe == null) return ProductionOperationResult.InvalidRecipe;

            // Check storage capacity
            if (!_inventoryManager.CanAddItem(recipe.OutputQuantity))
            {
                OnNotificationMessage?.Invoke("Storage Full");
                return ProductionOperationResult.StorageFull;
            }

            // Add item to inventory
            _inventoryManager.AddItem(recipe.OutputItemId, recipe.Name, ItemType.ManufacturedGood, recipe.OutputQuantity);

            // Award XP once
            if (!headJob.XpAwarded)
            {
                headJob.XpAwarded = true;
                _playerProfile.AddXP(recipe.XpReward);
            }

            // Remove completed job
            b.JobsQueue.RemoveAt(0);

            // Trigger next queued job
            UpdateSingleBuildingProduction(b);

            OnProductionStateChanged?.Invoke(b);
            OnNotificationMessage?.Invoke($"Collected +{recipe.OutputQuantity} {recipe.Name} (+{recipe.XpReward} XP)");
            return ProductionOperationResult.Success;
        }

        public bool CancelQueuedJob(string buildingInstanceId, string jobId)
        {
            var b = GetProductionBuilding(buildingInstanceId);
            if (b == null || b.JobsQueue == null || b.JobsQueue.Count <= 1) return false;

            // Cannot cancel active job [0] that has already consumed ingredients; only queued jobs [1..N]
            for (int i = 1; i < b.JobsQueue.Count; i++)
            {
                if (b.JobsQueue[i].JobId == jobId)
                {
                    b.JobsQueue.RemoveAt(i);
                    OnProductionStateChanged?.Invoke(b);
                    OnNotificationMessage?.Invoke("Queued job cancelled");
                    return true;
                }
            }
            return false;
        }

        public void DevInstantCompleteCurrentJob(string buildingInstanceId)
        {
            var b = GetProductionBuilding(buildingInstanceId);
            if (b != null && b.JobsQueue != null && b.JobsQueue.Count > 0)
            {
                var activeJob = b.JobsQueue[0];
                if (activeJob.State == ProductionJobState.Producing || activeJob.State == ProductionJobState.Queued)
                {
                    activeJob.State = ProductionJobState.Ready;
                    OnProductionStateChanged?.Invoke(b);
                }
            }
        }
    }
}
