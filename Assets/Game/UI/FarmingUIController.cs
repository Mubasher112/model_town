using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Farming;
using Game.Data;
using Game.Inventory;
using Game.Player;

namespace Game.UI
{
    public class FarmingUIController : MonoBehaviour
    {
        [Header("Seed Selection Panel")]
        [SerializeField] private GameObject seedSelectionPanel;
        [SerializeField] private Transform cropListParent;

        [Header("Crop Growth Panel")]
        [SerializeField] private GameObject cropTimerPanel;
        [SerializeField] private Text cropNameText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text notificationText;

        private FarmManager _farmManager;
        private InventoryManager _inventoryManager;
        private PlayerProfile _playerProfile;

        private string _selectedFieldId;

        public void Initialize(FarmManager farmManager, InventoryManager inventoryManager, PlayerProfile playerProfile)
        {
            _farmManager = farmManager;
            _inventoryManager = inventoryManager;
            _playerProfile = playerProfile;

            if (_farmManager != null)
            {
                _farmManager.OnNotificationMessage += ShowNotification;
            }
        }

        private void OnDestroy()
        {
            if (_farmManager != null)
            {
                _farmManager.OnNotificationMessage -= ShowNotification;
            }
        }

        public void OnFieldSelected(FieldInstance field)
        {
            if (field == null)
            {
                CloseAllPanels();
                return;
            }

            _selectedFieldId = field.FieldId;

            if (field.State == FieldState.Empty)
            {
                ShowSeedSelectionPanel();
            }
            else if (field.State == FieldState.Growing || field.State == FieldState.Planted)
            {
                ShowCropTimerPanel(field);
            }
            else if (field.State == FieldState.Ready)
            {
                _farmManager.HarvestCrop(_selectedFieldId);
                CloseAllPanels();
            }
        }

        public void ShowSeedSelectionPanel()
        {
            if (seedSelectionPanel != null) seedSelectionPanel.SetActive(true);
            if (cropTimerPanel != null) cropTimerPanel.SetActive(false);
        }

        public void SelectCropToPlant(string cropId)
        {
            if (!string.IsNullOrEmpty(_selectedFieldId) && _farmManager != null)
            {
                var result = _farmManager.PlantCrop(_selectedFieldId, cropId);
                if (result == FarmingOperationResult.Success)
                {
                    CloseAllPanels();
                }
            }
        }

        public void ShowCropTimerPanel(FieldInstance field)
        {
            if (seedSelectionPanel != null) seedSelectionPanel.SetActive(false);
            if (cropTimerPanel != null) cropTimerPanel.SetActive(true);

            var crop = CropLibrary.GetCrop(field.CurrentCropId);
            if (cropNameText != null && crop != null) cropNameText.text = crop.Name;
        }

        public void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }
        }

        public void CloseAllPanels()
        {
            if (seedSelectionPanel != null) seedSelectionPanel.SetActive(false);
            if (cropTimerPanel != null) cropTimerPanel.SetActive(false);
            _selectedFieldId = null;
        }
    }
}
