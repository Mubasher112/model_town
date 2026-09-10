using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Residents;
using Game.Player;
using Game.Buildings;

namespace Game.UI
{
    public class TownOverviewUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject overviewPanel;

        [Header("Stats UI Elements")]
        [SerializeField] private Text populationText;
        [SerializeField] private Text happinessText;
        [SerializeField] private Text housingText;
        [SerializeField] private Text communityText;
        [SerializeField] private Text townLevelText;

        private PopulationManager _populationManager;
        private PlayerProfile _playerProfile;
        private BuildingManager _buildingManager;

        public void Initialize(PopulationManager populationManager, PlayerProfile playerProfile, BuildingManager buildingManager)
        {
            _populationManager = populationManager;
            _playerProfile = playerProfile;
            _buildingManager = buildingManager;

            if (_populationManager != null)
            {
                _populationManager.OnPopulationUpdated += RefreshUI;
            }
        }

        private void OnDestroy()
        {
            if (_populationManager != null)
            {
                _populationManager.OnPopulationUpdated -= RefreshUI;
            }
        }

        public void OpenOverviewPanel()
        {
            if (overviewPanel != null) overviewPanel.SetActive(true);
            RefreshUI();
        }

        public void CloseOverviewPanel()
        {
            if (overviewPanel != null) overviewPanel.SetActive(false);
        }

        public void RefreshUI()
        {
            if (_populationManager == null) return;

            var stats = _populationManager.GetPopulationStats();

            if (populationText != null) populationText.text = $"{stats.CurrentPopulation} / {stats.TotalHousingCapacity}";
            if (happinessText != null) happinessText.text = $"{stats.HappinessScore}% ({stats.HappinessRating})";
            if (housingText != null) housingText.text = $"{stats.ResidentialHousesCount} Houses | {stats.OccupiedHousesCount} Occupied";
            if (townLevelText != null && _playerProfile != null) townLevelText.text = $"Level {_playerProfile.Level}";
        }
    }
}
