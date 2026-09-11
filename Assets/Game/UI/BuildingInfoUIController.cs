using System;
using UnityEngine;
using UnityEngine.UI;
using Game.Buildings;
using Game.Data;
using Game.Economy;
using Game.Player;
using Game.Residents;

namespace Game.UI
{
    public class BuildingInfoUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private Text buildingNameText;
        [SerializeField] private Text buildingLevelText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text occupancyText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button removeButton;

        private BuildingManager _buildingManager;
        private EconomyManager _economyManager;
        private PlayerProfile _playerProfile;
        private PopulationManager _populationManager;

        private BuildingInstance _selectedBuilding;

        public void Initialize(
            BuildingManager buildingManager,
            EconomyManager economyManager,
            PlayerProfile playerProfile,
            PopulationManager populationManager = null)
        {
            _buildingManager = buildingManager;
            _economyManager = economyManager;
            _playerProfile = playerProfile;
            _populationManager = populationManager;
        }

        public void OnBuildingSelected(BuildingInstance building)
        {
            if (building == null)
            {
                ClosePanel();
                return;
            }

            _selectedBuilding = building;
            if (infoPanel != null) infoPanel.SetActive(true);

            var config = BuildingLibrary.GetBuilding(building.BuildingId);
            if (config == null) return;

            if (buildingNameText != null) buildingNameText.text = config.Name;
            if (buildingLevelText != null) buildingLevelText.text = $"Lvl {building.Level}";
            if (descriptionText != null) descriptionText.text = config.Description;

            if (statusText != null)
            {
                if (building.State == BuildingState.UnderConstruction)
                {
                    statusText.text = "Under Construction...";
                }
                else if (building.State == BuildingState.Upgrading)
                {
                    statusText.text = "Upgrading...";
                }
                else if (building.State == BuildingState.Completed)
                {
                    statusText.text = "Operational";
                }
            }

            if (occupancyText != null)
            {
                if (config.Category == BuildingCategory.Residential)
                {
                    int cap = config.PopulationCapacity + (building.Level - 1) * config.UpgradePopulationBonus;
                    int occupants = 0;
                    if (_populationManager != null && building.State == BuildingState.Completed)
                    {
                        var residents = _populationManager.GetAllResidents();
                        foreach (var r in residents)
                        {
                            if (r.AssignedHouseInstanceId == building.InstanceId) occupants++;
                        }
                    }
                    occupancyText.text = $"Residents: {occupants} / {cap} | Happiness: +{config.HappinessBonus}";
                }
                else
                {
                    occupancyText.text = string.Empty;
                }
            }
        }

        public void OnUpgradeClicked()
        {
            if (_selectedBuilding != null && _buildingManager != null)
            {
                _buildingManager.StartUpgrade(_selectedBuilding.InstanceId);
                OnBuildingSelected(_selectedBuilding);
            }
        }

        public void OnRemoveClicked()
        {
            if (_selectedBuilding != null && _buildingManager != null)
            {
                _buildingManager.RemoveBuilding(_selectedBuilding.InstanceId);
                ClosePanel();
            }
        }

        public void ClosePanel()
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            _selectedBuilding = null;
        }
    }
}
