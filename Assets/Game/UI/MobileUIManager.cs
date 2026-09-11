using UnityEngine;
using UnityEngine.UI;
using Game.Player;
using Game.Economy;
using Game.Inventory;

namespace Game.UI
{
    public class MobileUIManager : MonoBehaviour
    {
        [Header("Top Bar UI Elements")]
        [SerializeField] private Text levelText;
        [SerializeField] private Text xpText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text gemsText;

        [Header("Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject confirmationModal;

        private PlayerProfile _profile;

        public void BindPlayerProfile(PlayerProfile profile)
        {
            if (_profile != null)
            {
                _profile.OnProfileUpdated -= RefreshTopBarUI;
            }

            _profile = profile;

            if (_profile != null)
            {
                _profile.OnProfileUpdated += RefreshTopBarUI;
                RefreshTopBarUI();
            }
        }

        private void OnDestroy()
        {
            if (_profile != null)
            {
                _profile.OnProfileUpdated -= RefreshTopBarUI;
            }
        }

        public void RefreshTopBarUI()
        {
            if (_profile == null) return;

            if (levelText != null) levelText.text = $"Lvl {_profile.Level}";
            if (xpText != null) xpText.text = $"XP: {_profile.CurrentXP}/{_profile.RequiredXPForNextLevel}";
            if (coinsText != null) coinsText.text = $"{_profile.Coins}";
            if (gemsText != null) gemsText.text = $"{_profile.Gems}";
        }

        public void ToggleInventoryPanel()
        {
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            }
        }

        public void ShowConfirmationModal(bool show)
        {
            if (confirmationModal != null)
            {
                confirmationModal.SetActive(show);
            }
        }
    }
}
