using System;
using UnityEngine;
using UnityEngine.UI;
using Game.Adventure;
using Game.Data;

namespace Game.UI
{
    public class AdventureUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject adventureMapPanel;
        [SerializeField] private GameObject nodeDetailPanel;
        [SerializeField] private GameObject unlockModalPanel;

        [Header("Energy & Info Header")]
        [SerializeField] private Text energyText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text areaNameText;
        [SerializeField] private Text discoveryProgressText;

        [Header("Node Detail Modal")]
        [SerializeField] private Text nodeNameText;
        [SerializeField] private Text nodeDescriptionText;
        [SerializeField] private Text toolRequirementText;
        [SerializeField] private Text energyCostText;
        [SerializeField] private Text rewardText;
        [SerializeField] private Button actionButton; // MINE or CLEAR
        [SerializeField] private Text actionButtonText;

        [Header("Unlock Modal")]
        [SerializeField] private Text unlockRequirementsText;
        [SerializeField] private Button unlockButton;

        [Header("Notification")]
        [SerializeField] private Text notificationText;

        private AdventureManager _adventureManager;
        private AdventureNodeInstance _selectedNode;

        public void Initialize(AdventureManager adventureManager)
        {
            _adventureManager = adventureManager;
            CloseAllPanels();
        }

        private void Update()
        {
            if (_adventureManager == null) return;

            if (adventureMapPanel != null && adventureMapPanel.activeSelf)
            {
                UpdateEnergyDisplay();
            }
        }

        public void ShowAdventureEntryModal(int currentPopulation)
        {
            if (_adventureManager == null) return;

            if (_adventureManager.IsUnlocked)
            {
                OpenAdventureMap();
                return;
            }

            if (unlockModalPanel != null) unlockModalPanel.SetActive(true);

            var areaDef = _adventureManager.CurrentAreaDef;
            if (unlockRequirementsText != null && areaDef != null)
            {
                unlockRequirementsText.text = $"Unlock {areaDef.Name}\nRequired Level: {areaDef.RequiredLevel}\nCoins Cost: {areaDef.RequiredCoins:N0}\nPopulation: {areaDef.RequiredPopulation} (Current: {currentPopulation})";
            }
        }

        public void OnClickUnlockAdventureButton(int currentPopulation)
        {
            if (_adventureManager == null) return;

            var result = _adventureManager.TryUnlockAdventure(currentPopulation);
            if (result == AdventureOperationResult.Success)
            {
                ShowNotification("Ancient Valley Unlocked!");
                if (unlockModalPanel != null) unlockModalPanel.SetActive(false);
                OpenAdventureMap();
            }
            else
            {
                ShowNotification($"Cannot unlock adventure: {result}");
            }
        }

        public void OpenAdventureMap()
        {
            if (_adventureManager == null || !_adventureManager.IsUnlocked) return;

            _adventureManager.EnterAdventureArea();
            if (adventureMapPanel != null) adventureMapPanel.SetActive(true);
            if (areaNameText != null && _adventureManager.CurrentAreaDef != null)
            {
                areaNameText.text = _adventureManager.CurrentAreaDef.Name.ToUpper();
            }

            UpdateEnergyDisplay();
            UpdateDiscoveryProgress();
        }

        public void ExitAdventureMap()
        {
            if (_adventureManager != null)
            {
                _adventureManager.ExitAdventureArea();
            }

            CloseAllPanels();
        }

        public void SelectNodeAtPosition(Vector2Int gridPos)
        {
            if (_adventureManager == null) return;

            _selectedNode = null;
            foreach (var node in _adventureManager.ExportNodes())
            {
                if (node.GridPosition == gridPos && !node.IsCleared)
                {
                    _selectedNode = node;
                    break;
                }
            }

            if (_selectedNode == null)
            {
                if (nodeDetailPanel != null) nodeDetailPanel.SetActive(false);
                return;
            }

            ShowNodeDetailModal(_selectedNode);
        }

        private void ShowNodeDetailModal(AdventureNodeInstance node)
        {
            var def = ResourceNodeLibrary.GetDefinition(node.NodeDefId);
            if (def == null) return;

            if (nodeDetailPanel != null) nodeDetailPanel.SetActive(true);

            if (nodeNameText != null) nodeNameText.text = def.Name;
            if (nodeDescriptionText != null)
            {
                nodeDescriptionText.text = $"Available Quantity: {node.CurrentQuantity} / {node.MaxQuantity}";
            }

            if (toolRequirementText != null)
            {
                string toolName = string.IsNullOrEmpty(def.RequiredToolId) ? "None" : ToolLibrary.GetTool(def.RequiredToolId)?.Name ?? def.RequiredToolId;
                var toolInst = _adventureManager.ToolService.GetTool(def.RequiredToolId);
                int dur = toolInst != null ? toolInst.CurrentDurability : 0;
                toolRequirementText.text = $"Tool: {toolName} (Durability: {dur})";
            }

            if (energyCostText != null) energyCostText.text = $"⚡ Energy: {def.EnergyCost}";
            if (rewardText != null) rewardText.text = $"Reward: +{def.YieldQuantity} Resources, +{def.XpReward} XP";

            if (actionButtonText != null)
            {
                actionButtonText.text = def.Type == AdventureNodeType.Obstacle ? "CLEAR OBSTACLE" : "MINE RESOURCE";
            }
        }

        public void OnClickGatherSelectedNode()
        {
            if (_selectedNode == null || _adventureManager == null) return;

            var result = _adventureManager.GatherNodeAtPosition(_selectedNode.GridPosition, out int yieldAmount, out int xpEarned);
            if (result == GatheringOperationResult.Success)
            {
                ShowNotification($"Gathered {yieldAmount} resources! (+{xpEarned} XP)");
                ShowNodeDetailModal(_selectedNode);
                UpdateEnergyDisplay();
                UpdateDiscoveryProgress();
            }
            else
            {
                ShowNotification($"Action failed: {result}");
            }
        }

        private void UpdateEnergyDisplay()
        {
            if (_adventureManager == null || _adventureManager.EnergyManager == null) return;

            var energy = _adventureManager.EnergyManager;
            if (energyText != null)
            {
                energyText.text = $"⚡ {energy.CurrentEnergy} / {energy.MaxEnergy}";
            }

            if (timerText != null)
            {
                float remainingSeconds = energy.GetSecondsToNextEnergy();
                if (remainingSeconds > 0)
                {
                    TimeSpan span = TimeSpan.FromSeconds(remainingSeconds);
                    timerText.text = $"+1 ⚡ in {span.Minutes:D2}:{span.Seconds:D2}";
                }
                else
                {
                    timerText.text = "⚡ Full";
                }
            }
        }

        private void UpdateDiscoveryProgress()
        {
            if (_adventureManager == null || _adventureManager.ExplorationService == null) return;

            float progress = _adventureManager.ExplorationService.GetDiscoveredPercentage(25, 25);
            if (discoveryProgressText != null)
            {
                discoveryProgressText.text = $"Explored: {progress * 100:F0}%";
            }
        }

        private void ShowNotification(string msg)
        {
            if (notificationText != null)
            {
                notificationText.text = msg;
                CancelInvoke(nameof(ClearNotification));
                Invoke(nameof(ClearNotification), 3.0f);
            }
        }

        private void ClearNotification()
        {
            if (notificationText != null) notificationText.text = string.Empty;
        }

        public void CloseAllPanels()
        {
            if (adventureMapPanel != null) adventureMapPanel.SetActive(false);
            if (nodeDetailPanel != null) nodeDetailPanel.SetActive(false);
            if (unlockModalPanel != null) unlockModalPanel.SetActive(false);
        }
    }
}
