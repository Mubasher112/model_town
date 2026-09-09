using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Data;
using Game.Economy;
using Game.Inventory;
using Game.Player;
using Game.World;

namespace Game.UI
{
    public class BuildMenuUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject buildMenuPanel;
        [SerializeField] private Text categoryTitleText;
        [SerializeField] private Transform buildingListParent;

        private BuildingManager _buildingManager;
        private PlayerProfile _playerProfile;
        private EconomyManager _economyManager;
        private InventoryManager _inventoryManager;

        private BuildingCategory _currentCategory = BuildingCategory.Residential;

        public event Action<string> OnBuildingSelectedForPlacement;

        public void Initialize(BuildingManager buildingManager, PlayerProfile playerProfile, EconomyManager economyManager, InventoryManager inventoryManager)
        {
            _buildingManager = buildingManager;
            _playerProfile = playerProfile;
            _economyManager = economyManager;
            _inventoryManager = inventoryManager;
        }

        public void OpenMenu()
        {
            if (buildMenuPanel != null) buildMenuPanel.SetActive(true);
            SelectCategory(BuildingCategory.Residential);
        }

        public void CloseMenu()
        {
            if (buildMenuPanel != null) buildMenuPanel.SetActive(false);
        }

        public void SelectCategory(BuildingCategory category)
        {
            _currentCategory = category;
            if (categoryTitleText != null) categoryTitleText.text = category.ToString();
        }

        public void OnBuildingCardClicked(string buildingId)
        {
            var config = BuildingLibrary.GetBuilding(buildingId);
            if (config == null) return;

            if (_playerProfile.Level < config.UnlockLevel)
            {
                return;
            }

            if (!_economyManager.CanAffordCoins(config.BuildCostCoins))
            {
                return;
            }

            CloseMenu();
            OnBuildingSelectedForPlacement?.Invoke(buildingId);
        }
    }
}
