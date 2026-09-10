using System;
using UnityEngine;
using UnityEngine.UI;
using Game.World;
using Game.Economy;
using Game.Player;
using Game.Residents;

namespace Game.UI
{
    public class LandExpansionUIController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject expansionPanel;
        [SerializeField] private Text areaSizeText;
        [SerializeField] private Text costText;
        [SerializeField] private Text levelReqText;
        [SerializeField] private Text popReqText;
        [SerializeField] private Text statusText;
        [SerializeField] private Button expandButton;

        private LandExpansionManager _expansionManager;
        private EconomyManager _economyManager;
        private PlayerProfile _playerProfile;
        private PopulationManager _populationManager;

        private LandExpansionZone _selectedZone;

        public void Initialize(
            LandExpansionManager expansionManager,
            EconomyManager economyManager,
            PlayerProfile playerProfile,
            PopulationManager populationManager)
        {
            _expansionManager = expansionManager;
            _economyManager = economyManager;
            _playerProfile = playerProfile;
            _populationManager = populationManager;
        }

        public void OnLockedLandSelected(LandExpansionZone zone)
        {
            if (zone == null || zone.IsUnlocked)
            {
                ClosePanel();
                return;
            }

            _selectedZone = zone;
            if (expansionPanel != null) expansionPanel.SetActive(true);

            int width = zone.MaxCoord.x - zone.MinCoord.x + 1;
            int height = zone.MaxCoord.y - zone.MinCoord.y + 1;

            if (areaSizeText != null) areaSizeText.text = $"Area: {width} x {height}";
            if (costText != null) costText.text = $"Cost: {zone.CostCoins} Coins";
            if (levelReqText != null) levelReqText.text = $"Required Level: {zone.RequiredLevel} (Current: {_playerProfile?.Level ?? 1})";
            if (popReqText != null) popReqText.text = $"Required Pop: {zone.RequiredPopulation}";

            bool levelMet = _playerProfile != null && _playerProfile.Level >= zone.RequiredLevel;
            bool coinsMet = _economyManager != null && _economyManager.CanAffordCoins(zone.CostCoins);
            int currentPop = _populationManager != null ? _populationManager.GetAllResidents().Count : 0;
            bool popMet = currentPop >= zone.RequiredPopulation;

            if (statusText != null)
            {
                if (!levelMet) statusText.text = $"Locked (Level {zone.RequiredLevel} required)";
                else if (!popMet) statusText.text = $"Locked (Population {zone.RequiredPopulation} required)";
                else if (!coinsMet) statusText.text = "Not enough coins";
                else statusText.text = "Ready to Expand!";
            }

            if (expandButton != null)
            {
                expandButton.interactable = levelMet && popMet && coinsMet;
            }
        }

        public void OnExpandClicked()
        {
            if (_selectedZone != null && _expansionManager != null)
            {
                int currentPop = _populationManager != null ? _populationManager.GetAllResidents().Count : 0;
                var res = _expansionManager.TryPurchaseExpansion(_selectedZone.ZoneId, _playerProfile, _economyManager, currentPop);
                if (res == ExpansionOperationResult.Success)
                {
                    ClosePanel();
                }
                else
                {
                    OnLockedLandSelected(_selectedZone);
                }
            }
        }

        public void ClosePanel()
        {
            if (expansionPanel != null) expansionPanel.SetActive(false);
            _selectedZone = null;
        }
    }
}
