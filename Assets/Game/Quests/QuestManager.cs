using System;
using System.Collections.Generic;
using Game.Data;
using Game.Economy;
using Game.Inventory;
using Game.Player;
using Game.Services;

namespace Game.Quests
{
    public enum QuestClaimResult
    {
        Success,
        QuestNotFound,
        NotCompleted,
        AlreadyClaimed,
        StorageFull
    }

    public class QuestManager
    {
        private readonly PlayerProfile _playerProfile;
        private readonly EconomyManager _economyManager;
        private readonly InventoryManager _inventoryManager;
        private readonly IGameTimeService _timeService;

        private readonly Dictionary<string, QuestInstance> _activeQuests = new Dictionary<string, QuestInstance>();
        private readonly HashSet<string> _claimedQuestIds = new HashSet<string>();
        private readonly List<string> _history = new List<string>();

        private long _lastDailyResetUtcTicks;

        public event Action OnQuestsUpdated;
        public event Action<string> OnNotificationMessage;

        public QuestManager(
            PlayerProfile playerProfile,
            EconomyManager economyManager,
            InventoryManager inventoryManager,
            IGameTimeService timeService,
            List<QuestInstance> savedQuests = null,
            IEnumerable<string> savedClaimedIds = null,
            long lastDailyResetUtcTicks = 0)
        {
            _playerProfile = playerProfile ?? throw new ArgumentNullException(nameof(playerProfile));
            _economyManager = economyManager ?? throw new ArgumentNullException(nameof(economyManager));
            _inventoryManager = inventoryManager ?? throw new ArgumentNullException(nameof(inventoryManager));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _lastDailyResetUtcTicks = lastDailyResetUtcTicks > 0 ? lastDailyResetUtcTicks : _timeService.CurrentUtcTicks;

            if (savedClaimedIds != null)
            {
                foreach (var id in savedClaimedIds) _claimedQuestIds.Add(id);
            }

            if (savedQuests != null && savedQuests.Count > 0)
            {
                foreach (var q in savedQuests)
                {
                    _activeQuests[q.QuestId] = q;
                }
            }

            CheckDailyReset();
            EvaluateAvailableQuests();
        }

        public void CheckDailyReset()
        {
            long now = _timeService.CurrentUtcTicks;
            long elapsedTicks = now - _lastDailyResetUtcTicks;

            if (TimeSpan.FromTicks(elapsedTicks).TotalHours >= 24)
            {
                // Reset Daily Goals
                _lastDailyResetUtcTicks = now;
                foreach (var def in QuestLibrary.GetQuestsByType(QuestType.Daily))
                {
                    _claimedQuestIds.Remove(def.QuestId);
                    _activeQuests.Remove(def.QuestId);
                }
                EvaluateAvailableQuests();
                OnNotificationMessage?.Invoke("Daily Goals Reset!");
            }
        }

        public void EvaluateAvailableQuests()
        {
            long now = _timeService.CurrentUtcTicks;
            bool updated = false;

            foreach (var def in QuestLibrary.GetAllQuests())
            {
                if (_claimedQuestIds.Contains(def.QuestId)) continue;

                if (_playerProfile.Level < def.RequiredLevel) continue;

                // Check Prerequisite
                if (!string.IsNullOrEmpty(def.PrerequisiteQuestId))
                {
                    if (!_claimedQuestIds.Contains(def.PrerequisiteQuestId)) continue;
                }

                if (!_activeQuests.TryGetValue(def.QuestId, out var instance))
                {
                    var progresses = new List<QuestObjectiveProgress>();
                    foreach (var objDef in def.Objectives)
                    {
                        progresses.Add(new QuestObjectiveProgress(objDef.ObjectiveId, objDef.TargetAmount));
                    }

                    instance = new QuestInstance(def.QuestId, QuestState.Active, progresses, now, def.ExpirationDurationSeconds);
                    _activeQuests[def.QuestId] = instance;
                    updated = true;
                }
                else
                {
                    if (instance.State == QuestState.Locked)
                    {
                        instance.State = QuestState.Active;
                        updated = true;
                    }
                }
            }

            if (updated)
            {
                OnQuestsUpdated?.Invoke();
            }
        }

        public void OnGameplayEvent(ObjectiveType type, string targetId, int amount = 1)
        {
            CheckDailyReset();
            bool updated = false;

            foreach (var instance in _activeQuests.Values)
            {
                if (instance.State != QuestState.Active) continue;

                var def = QuestLibrary.GetQuest(instance.QuestId);
                if (def == null) continue;

                bool allCompleted = true;
                for (int i = 0; i < def.Objectives.Count; i++)
                {
                    var objDef = def.Objectives[i];
                    var objProg = instance.ObjectivesProgress.Find(p => p.ObjectiveId == objDef.ObjectiveId);
                    if (objProg == null) continue;

                    if (!objProg.IsCompleted)
                    {
                        if (objDef.Type == type)
                        {
                            if (string.IsNullOrEmpty(objDef.TargetId) || objDef.TargetId == targetId)
                            {
                                if (type == ObjectiveType.Population || type == ObjectiveType.Happiness)
                                {
                                    objProg.CurrentAmount = amount; // Absolute target threshold
                                }
                                else
                                {
                                    objProg.CurrentAmount += amount; // Incremental progress
                                }

                                if (objProg.CurrentAmount >= objProg.TargetAmount)
                                {
                                    objProg.CurrentAmount = objProg.TargetAmount;
                                    objProg.IsCompleted = true;
                                    OnNotificationMessage?.Invoke($"Objective Complete: {objDef.Description}");
                                }
                                updated = true;
                            }
                        }
                    }

                    if (!objProg.IsCompleted)
                    {
                        allCompleted = false;
                    }
                }

                if (allCompleted && instance.State == QuestState.Active)
                {
                    instance.State = QuestState.Completed;
                    OnNotificationMessage?.Invoke($"Quest Complete: {def.Title}! Reward Ready!");
                    updated = true;
                }
            }

            if (updated)
            {
                OnQuestsUpdated?.Invoke();
            }
        }

        public QuestClaimResult ClaimReward(string questId)
        {
            if (_claimedQuestIds.Contains(questId)) return QuestClaimResult.AlreadyClaimed;

            if (!_activeQuests.TryGetValue(questId, out var instance)) return QuestClaimResult.QuestNotFound;

            if (instance.State != QuestState.Completed) return QuestClaimResult.NotCompleted;

            var def = QuestLibrary.GetQuest(questId);
            if (def == null) return QuestClaimResult.QuestNotFound;

            // Storage capacity validation if reward includes items
            if (!string.IsNullOrEmpty(def.Reward.RewardItemId) && def.Reward.RewardItemQuantity > 0)
            {
                if (!_inventoryManager.CanAddItem(def.Reward.RewardItemQuantity))
                {
                    OnNotificationMessage?.Invoke("Storage is full! Free storage to claim reward.");
                    return QuestClaimResult.StorageFull;
                }
            }

            // ATOMIC REWARD CLAIMING
            instance.State = QuestState.Claimed;
            _claimedQuestIds.Add(questId);

            if (def.Reward.Coins > 0) _economyManager.EarnCoins(def.Reward.Coins);
            if (def.Reward.Xp > 0) _playerProfile.AddXP(def.Reward.Xp);

            if (!string.IsNullOrEmpty(def.Reward.RewardItemId) && def.Reward.RewardItemQuantity > 0)
            {
                _inventoryManager.AddItem(def.Reward.RewardItemId, def.Reward.RewardItemId, ItemType.RawMaterial, def.Reward.RewardItemQuantity);
            }

            _history.Add(questId);
            _activeQuests.Remove(questId);

            EvaluateAvailableQuests();

            OnNotificationMessage?.Invoke($"Claimed Reward: +{def.Reward.Coins} Coins, +{def.Reward.Xp} XP!");
            OnQuestsUpdated?.Invoke();

            return QuestClaimResult.Success;
        }

        public bool HasUnclaimedRewards()
        {
            foreach (var q in _activeQuests.Values)
            {
                if (q.State == QuestState.Completed) return true;
            }
            return false;
        }

        public List<QuestInstance> GetActiveQuests() => new List<QuestInstance>(_activeQuests.Values);
        public List<string> GetClaimedQuestIds() => new List<string>(_claimedQuestIds);
        public long LastDailyResetUtcTicks => _lastDailyResetUtcTicks;

        public void DevCompleteObjective(string questId)
        {
            if (_activeQuests.TryGetValue(questId, out var instance))
            {
                foreach (var p in instance.ObjectivesProgress)
                {
                    p.CurrentAmount = p.TargetAmount;
                    p.IsCompleted = true;
                }
                instance.State = QuestState.Completed;
                OnQuestsUpdated?.Invoke();
            }
        }

        public void DevClaimReward(string questId)
        {
            ClaimReward(questId);
        }
    }
}
