using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Production;
using Game.Buildings;
using Game.Data;
using Game.Inventory;
using Game.Player;

namespace Game.UI
{
    public class ProductionUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject productionPanel;
        [SerializeField] private Text buildingNameText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text notificationText;

        private ProductionManager _productionManager;
        private BuildingManager _buildingManager;
        private InventoryManager _inventoryManager;
        private PlayerProfile _playerProfile;

        private ProductionBuildingInstance _selectedBuilding;

        public void Initialize(
            ProductionManager productionManager,
            BuildingManager buildingManager,
            InventoryManager inventoryManager,
            PlayerProfile playerProfile)
        {
            _productionManager = productionManager;
            _buildingManager = buildingManager;
            _inventoryManager = inventoryManager;
            _playerProfile = playerProfile;

            if (_productionManager != null)
            {
                _productionManager.OnNotificationMessage += ShowNotification;
                _productionManager.OnProductionStateChanged += OnProductionBuildingStateUpdated;
            }
        }

        private void OnDestroy()
        {
            if (_productionManager != null)
            {
                _productionManager.OnNotificationMessage -= ShowNotification;
                _productionManager.OnProductionStateChanged -= OnProductionBuildingStateUpdated;
            }
        }

        public void OnProductionBuildingSelected(ProductionBuildingInstance building)
        {
            if (building == null)
            {
                ClosePanel();
                return;
            }

            _selectedBuilding = building;
            if (productionPanel != null) productionPanel.SetActive(true);

            RefreshUI();
        }

        private void OnProductionBuildingStateUpdated(ProductionBuildingInstance building)
        {
            if (_selectedBuilding != null && building != null && _selectedBuilding.BuildingInstanceId == building.BuildingInstanceId)
            {
                RefreshUI();
            }
        }

        private void RefreshUI()
        {
            if (_selectedBuilding == null) return;

            var bConfig = BuildingLibrary.GetBuilding(_selectedBuilding.BuildingId);
            if (buildingNameText != null) buildingNameText.text = bConfig != null ? bConfig.Name : "Factory";

            var activeJob = _selectedBuilding.CurrentActiveJob;
            if (statusText != null)
            {
                if (activeJob == null)
                {
                    statusText.text = "Idle";
                }
                else if (activeJob.State == ProductionJobState.Producing)
                {
                    var recipe = RecipeLibrary.GetRecipe(activeJob.RecipeId);
                    statusText.text = $"Producing {recipe?.Name}...";
                }
                else if (activeJob.State == ProductionJobState.Ready)
                {
                    statusText.text = "Product Ready! Tap Collect";
                }
                else if (activeJob.State == ProductionJobState.Blocked)
                {
                    statusText.text = "Blocked: Missing Ingredients";
                }
            }
        }

        public void OnStartRecipeClicked(string recipeId)
        {
            if (_selectedBuilding != null && _productionManager != null)
            {
                _productionManager.StartProductionJob(_selectedBuilding.BuildingInstanceId, recipeId);
                RefreshUI();
            }
        }

        public void OnCollectClicked()
        {
            if (_selectedBuilding != null && _productionManager != null)
            {
                _productionManager.CollectProduct(_selectedBuilding.BuildingInstanceId);
                RefreshUI();
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
            }
        }

        public void ClosePanel()
        {
            if (productionPanel != null) productionPanel.SetActive(false);
            _selectedBuilding = null;
        }
    }
}
