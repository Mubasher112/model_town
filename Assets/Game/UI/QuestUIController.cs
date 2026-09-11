using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Quests;
using Game.Data;

namespace Game.UI
{
    public class QuestUIController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject questMainPanel;
        [SerializeField] private GameObject mainTabPanel;
        [SerializeField] private GameObject sideTabPanel;
        [SerializeField] private GameObject dailyTabPanel;
        [SerializeField] private GameObject milestoneTabPanel;

        [Header("Quest Card Displays")]
        [SerializeField] private Text questTitleText;
        [SerializeField] private Text questDescriptionText;
        [SerializeField] private Text questObjectivesText;
        [SerializeField] private Text questRewardsText;
        [SerializeField] private Button claimRewardButton;
        [SerializeField] private Text claimButtonText;

        [Header("Badge Indicator")]
        [SerializeField] private GameObject questNotificationBadge;

        [Header("Notifications")]
        [SerializeField] private Text notificationText;

        private QuestManager _questManager;
        private QuestType _activeTab = QuestType.Main;
        private QuestInstance _selectedQuest;

        public void Initialize(QuestManager questManager)
        {
            _questManager = questManager;
            if (_questManager != null)
            {
                _questManager.OnQuestsUpdated += UpdateBadgeAndUI;
            }
            CloseAllPanels();
            UpdateBadgeAndUI();
        }

        private void OnDestroy()
        {
            if (_questManager != null)
            {
                _questManager.OnQuestsUpdated -= UpdateBadgeAndUI;
            }
        }

        public void OpenQuestPanel()
        {
            CloseAllPanels();
            if (questMainPanel != null) questMainPanel.SetActive(true);
            OpenTab(QuestType.Main);
        }

        public void OpenTab(QuestType type)
        {
            _activeTab = type;
            if (mainTabPanel != null) mainTabPanel.SetActive(type == QuestType.Main);
            if (sideTabPanel != null) sideTabPanel.SetActive(type == QuestType.Side);
            if (dailyTabPanel != null) dailyTabPanel.SetActive(type == QuestType.Daily);
            if (milestoneTabPanel != null) milestoneTabPanel.SetActive(type == QuestType.Milestone);

            UpdateTabDisplay();
        }

        private void UpdateTabDisplay()
        {
            if (_questManager == null) return;

            var activeQuests = _questManager.GetActiveQuests();
            QuestInstance match = null;

            foreach (var q in activeQuests)
            {
                var def = QuestLibrary.GetQuest(q.QuestId);
                if (def != null && def.Type == _activeTab)
                {
                    match = q;
                    break;
                }
            }

            _selectedQuest = match;
            UpdateSelectedQuestDisplay();
        }

        private void UpdateSelectedQuestDisplay()
        {
            if (_selectedQuest == null)
            {
                if (questTitleText != null) questTitleText.text = "No Active Quests";
                if (questDescriptionText != null) questDescriptionText.text = "Check back later for new goals!";
                if (questObjectivesText != null) questObjectivesText.text = string.Empty;
                if (questRewardsText != null) questRewardsText.text = string.Empty;
                if (claimRewardButton != null) claimRewardButton.gameObject.SetActive(false);
                return;
            }

            var def = QuestLibrary.GetQuest(_selectedQuest.QuestId);
            if (def == null) return;

            if (questTitleText != null) questTitleText.text = def.Title;
            if (questDescriptionText != null) questDescriptionText.text = def.Description;

            if (questObjectivesText != null)
            {
                string objStr = "";
                foreach (var objProg in _selectedQuest.ObjectivesProgress)
                {
                    var objDef = def.Objectives.Find(o => o.ObjectiveId == objProg.ObjectiveId);
                    string desc = objDef != null ? objDef.Description : objProg.ObjectiveId;
                    string checkMark = objProg.IsCompleted ? " ✓" : "";
                    objStr += $"{desc}: {objProg.CurrentAmount} / {objProg.TargetAmount}{checkMark}\n";
                }
                questObjectivesText.text = objStr;
            }

            if (questRewardsText != null)
            {
                questRewardsText.text = $"Reward: +{def.Reward.Coins} Coins, +{def.Reward.Xp} XP";
            }

            if (claimRewardButton != null)
            {
                bool isCompleted = _selectedQuest.State == QuestState.Completed;
                claimRewardButton.gameObject.SetActive(isCompleted);
                if (claimButtonText != null)
                {
                    claimButtonText.text = isCompleted ? "CLAIM REWARD" : "IN PROGRESS";
                }
            }
        }

        public void OnClickClaimReward()
        {
            if (_selectedQuest == null || _questManager == null) return;

            var result = _questManager.ClaimReward(_selectedQuest.QuestId);
            if (result == QuestClaimResult.Success)
            {
                ShowNotification("Quest Reward Claimed!");
                UpdateTabDisplay();
                UpdateBadgeAndUI();
            }
            else
            {
                ShowNotification($"Cannot claim reward: {result}");
            }
        }

        private void UpdateBadgeAndUI()
        {
            if (_questManager == null) return;

            bool hasClaimable = _questManager.HasUnclaimedRewards();
            if (questNotificationBadge != null)
            {
                questNotificationBadge.SetActive(hasClaimable);
            }

            if (questMainPanel != null && questMainPanel.activeSelf)
            {
                UpdateTabDisplay();
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
            if (questMainPanel != null) questMainPanel.SetActive(false);
            if (mainTabPanel != null) mainTabPanel.SetActive(false);
            if (sideTabPanel != null) sideTabPanel.SetActive(false);
            if (dailyTabPanel != null) dailyTabPanel.SetActive(false);
            if (milestoneTabPanel != null) milestoneTabPanel.SetActive(false);
        }
    }
}
