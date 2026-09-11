using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Inventory;
using Game.Player;
using Game.Services;

namespace Game.Farming
{
    public enum FarmingOperationResult
    {
        Success,
        InvalidField,
        FieldNotEmpty,
        FieldNotReady,
        CropLocked,
        MissingSeed,
        StorageFull,
        InvalidCrop
    }

    public class FarmManager
    {
        private readonly Dictionary<string, FieldInstance> _fields = new Dictionary<string, FieldInstance>();
        private readonly InventoryManager _inventoryManager;
        private readonly PlayerProfile _playerProfile;
        private readonly StandardGameTimeService _timeService;

        public event Action<FieldInstance> OnFieldStateChanged;
        public event Action<string, int> OnCropPlanted;
        public event Action<string, int> OnCropHarvested;
        public event Action<string> OnNotificationMessage;

        public FarmManager(InventoryManager inventoryManager, PlayerProfile playerProfile, StandardGameTimeService timeService)
        {
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        public void RegisterField(FieldInstance field)
        {
            if (field == null) return;
            _fields[field.FieldId] = field;
            UpdateSingleFieldState(field);
        }

        public void UnregisterField(string fieldId)
        {
            if (_fields.ContainsKey(fieldId))
            {
                _fields.Remove(fieldId);
            }
        }

        public FieldInstance GetField(string fieldId)
        {
            return _fields.TryGetValue(fieldId, out var field) ? field : null;
        }

        public List<FieldInstance> GetAllFields()
        {
            return new List<FieldInstance>(_fields.Values);
        }

        public void UpdateSingleFieldState(FieldInstance field)
        {
            if (field == null) return;
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            if (field.State != FieldState.Empty && !string.IsNullOrEmpty(field.CurrentCropId))
            {
                var cropConfig = CropLibrary.GetCrop(field.CurrentCropId);
                if (field.CheckAndUpdateState(nowTicks, cropConfig))
                {
                    OnFieldStateChanged?.Invoke(field);
                }
            }
        }

        public void UpdateAllFieldStates()
        {
            _timeService.UpdateCurrentTime();
            long nowTicks = _timeService.CurrentUtcTicks;

            foreach (var field in _fields.Values)
            {
                if (field.State != FieldState.Empty && !string.IsNullOrEmpty(field.CurrentCropId))
                {
                    var cropConfig = CropLibrary.GetCrop(field.CurrentCropId);
                    if (field.CheckAndUpdateState(nowTicks, cropConfig))
                    {
                        OnFieldStateChanged?.Invoke(field);
                    }
                }
            }
        }

        public FarmingOperationResult PlantCrop(string fieldId, string cropId)
        {
            var field = GetField(fieldId);
            if (field == null) return FarmingOperationResult.InvalidField;
            if (field.State != FieldState.Empty) return FarmingOperationResult.FieldNotEmpty;

            var crop = CropLibrary.GetCrop(cropId);
            if (crop == null) return FarmingOperationResult.InvalidCrop;

            if (_playerProfile.Level < crop.UnlockLevel)
            {
                OnNotificationMessage?.Invoke($"Unlocks at Level {crop.UnlockLevel}");
                return FarmingOperationResult.CropLocked;
            }

            int seedCount = _inventoryManager.GetQuantity(crop.SeedItemId);
            if (seedCount < 1)
            {
                OnNotificationMessage?.Invoke("Insufficient seeds");
                return FarmingOperationResult.MissingSeed;
            }

            _inventoryManager.RemoveItem(crop.SeedItemId, 1);

            _timeService.UpdateCurrentTime();
            field.CurrentCropId = cropId;
            field.PlantedUtcTicks = _timeService.CurrentUtcTicks;
            field.State = FieldState.Planted;

            OnFieldStateChanged?.Invoke(field);
            OnCropPlanted?.Invoke(cropId, 1);
            OnNotificationMessage?.Invoke($"{crop.Name} planted!");
            return FarmingOperationResult.Success;
        }

        public FarmingOperationResult HarvestCrop(string fieldId)
        {
            var field = GetField(fieldId);
            if (field == null) return FarmingOperationResult.InvalidField;

            _timeService.UpdateCurrentTime();
            var crop = CropLibrary.GetCrop(field.CurrentCropId);
            field.CheckAndUpdateState(_timeService.CurrentUtcTicks, crop);

            if (field.State != FieldState.Ready)
            {
                return FarmingOperationResult.FieldNotReady;
            }

            if (crop == null) return FarmingOperationResult.InvalidCrop;

            if (!_inventoryManager.CanAddItem(crop.HarvestQuantity))
            {
                OnNotificationMessage?.Invoke("Storage Full");
                return FarmingOperationResult.StorageFull;
            }

            _inventoryManager.AddItem(crop.HarvestItemId, crop.Name, ItemType.Crop, crop.HarvestQuantity);

            _playerProfile.AddXP(crop.HarvestXp);

            string harvestedCropId = field.CurrentCropId;
            field.State = FieldState.Empty;
            field.CurrentCropId = null;
            field.PlantedUtcTicks = 0;

            OnFieldStateChanged?.Invoke(field);
            OnCropHarvested?.Invoke(harvestedCropId, crop.HarvestQuantity);
            OnNotificationMessage?.Invoke($"Harvested +{crop.HarvestQuantity} {crop.Name} (+{crop.HarvestXp} XP)");
            return FarmingOperationResult.Success;
        }

        public void DevInstantGrow(string fieldId)
        {
            var field = GetField(fieldId);
            if (field != null && field.State != FieldState.Empty)
            {
                field.State = FieldState.Ready;
                OnFieldStateChanged?.Invoke(field);
            }
        }
    }
}
