using System;
using System.Collections.Generic;

namespace Game.Data
{
    public enum QuestType
    {
        Main,
        Side,
        Daily,
        Milestone
    }

    public enum QuestState
    {
        Locked,
        Available,
        Active,
        Completed,
        Claimed,
        Expired
    }

    public enum ObjectiveType
    {
        Build,
        Upgrade,
        Plant,
        Harvest,
        Produce,
        Collect,
        Sell,
        Buy,
        Deliver,
        Population,
        Happiness,
        Expansion
    }

    [Serializable]
    public class QuestObjectiveDefinition
    {
        public string ObjectiveId;
        public ObjectiveType Type;
        public string TargetId; // e.g. "crop_wheat", "small_house", "feed_mill" or empty for general
        public int TargetAmount = 1;
        public string Description;

        public QuestObjectiveDefinition() { }

        public QuestObjectiveDefinition(string objectiveId, ObjectiveType type, string targetId, int targetAmount, string description)
        {
            ObjectiveId = objectiveId;
            Type = type;
            TargetId = targetId;
            TargetAmount = targetAmount;
            Description = description;
        }
    }

    [Serializable]
    public class QuestObjectiveProgress
    {
        public string ObjectiveId;
        public int CurrentAmount;
        public int TargetAmount;
        public bool IsCompleted;

        public QuestObjectiveProgress() { }

        public QuestObjectiveProgress(string objectiveId, int targetAmount)
        {
            ObjectiveId = objectiveId;
            CurrentAmount = 0;
            TargetAmount = targetAmount;
            IsCompleted = false;
        }
    }

    [Serializable]
    public class QuestReward
    {
        public long Coins;
        public int Xp;
        public string RewardItemId;
        public int RewardItemQuantity;

        public QuestReward() { }

        public QuestReward(long coins, int xp, string rewardItemId = null, int rewardItemQuantity = 0)
        {
            Coins = coins;
            Xp = xp;
            RewardItemId = rewardItemId;
            RewardItemQuantity = rewardItemQuantity;
        }
    }

    [Serializable]
    public class QuestDefinition
    {
        public string QuestId;
        public string Title;
        public string Description;
        public QuestType Type = QuestType.Main;
        public string PrerequisiteQuestId;
        public int RequiredLevel = 1;
        public List<QuestObjectiveDefinition> Objectives = new List<QuestObjectiveDefinition>();
        public QuestReward Reward = new QuestReward();
        public float ExpirationDurationSeconds = -1f; // -1 = no expiration

        public QuestDefinition() { }

        public QuestDefinition(
            string questId,
            string title,
            string description,
            QuestType type,
            string prerequisiteQuestId,
            int requiredLevel,
            List<QuestObjectiveDefinition> objectives,
            QuestReward reward,
            float expirationDurationSeconds = -1f)
        {
            QuestId = questId;
            Title = title;
            Description = description;
            Type = type;
            PrerequisiteQuestId = prerequisiteQuestId;
            RequiredLevel = requiredLevel;
            Objectives = objectives ?? new List<QuestObjectiveDefinition>();
            Reward = reward ?? new QuestReward();
            ExpirationDurationSeconds = expirationDurationSeconds;
        }
    }

    [Serializable]
    public class QuestInstance
    {
        public string QuestId;
        public QuestState State = QuestState.Locked;
        public List<QuestObjectiveProgress> ObjectivesProgress = new List<QuestObjectiveProgress>();
        public long CreatedUtcTicks;
        public long ExpirationUtcTicks;

        public QuestInstance() { }

        public QuestInstance(string questId, QuestState state, List<QuestObjectiveProgress> progress, long createdUtcTicks, float expirationDurationSeconds = -1f)
        {
            QuestId = questId;
            State = state;
            ObjectivesProgress = progress ?? new List<QuestObjectiveProgress>();
            CreatedUtcTicks = createdUtcTicks;

            if (expirationDurationSeconds > 0)
            {
                ExpirationUtcTicks = createdUtcTicks + TimeSpan.FromSeconds(expirationDurationSeconds).Ticks;
            }
            else
            {
                ExpirationUtcTicks = -1;
            }
        }
    }

    public static class QuestLibrary
    {
        private static readonly Dictionary<string, QuestDefinition> _quests = new Dictionary<string, QuestDefinition>
        {
            // Main Quest Chain
            {
                "main_q1_start_town",
                new QuestDefinition(
                    "main_q1_start_town",
                    "Start Your Town",
                    "Construct a Small House to welcome your first residents.",
                    QuestType.Main,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_build_house", ObjectiveType.Build, "small_house", 1, "Build 1 Small House")
                    },
                    new QuestReward(100, 20)
                )
            },
            {
                "main_q2_grow_crop",
                new QuestDefinition(
                    "main_q2_grow_crop",
                    "Grow Your First Crop",
                    "Plant and harvest wheat on your farm fields.",
                    QuestType.Main,
                    prerequisiteQuestId: "main_q1_start_town",
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_plant_wheat", ObjectiveType.Plant, "wheat", 1, "Plant 1 Wheat"),
                        new QuestObjectiveDefinition("obj_harvest_wheat", ObjectiveType.Harvest, "wheat", 1, "Harvest 1 Wheat")
                    },
                    new QuestReward(100, 25)
                )
            },
            {
                "main_q3_start_production",
                new QuestDefinition(
                    "main_q3_start_production",
                    "Start Production",
                    "Build a Feed Mill and produce animal feed.",
                    QuestType.Main,
                    prerequisiteQuestId: "main_q2_grow_crop",
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_build_feed_mill", ObjectiveType.Build, "feed_mill", 1, "Build 1 Feed Mill"),
                        new QuestObjectiveDefinition("obj_produce_feed", ObjectiveType.Produce, "recipe_animal_feed", 1, "Produce 1 Animal Feed")
                    },
                    new QuestReward(150, 40)
                )
            },
            {
                "main_q4_serve_customer",
                new QuestDefinition(
                    "main_q4_serve_customer",
                    "Serve a Customer",
                    "Fulfill customer orders to earn coins and experience.",
                    QuestType.Main,
                    prerequisiteQuestId: "main_q3_start_production",
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_fulfill_order", ObjectiveType.Deliver, null, 1, "Complete 1 Order")
                    },
                    new QuestReward(200, 50)
                )
            },
            {
                "main_q5_grow_population",
                new QuestDefinition(
                    "main_q5_grow_population",
                    "Grow the Town",
                    "Expand town population to 5 residents.",
                    QuestType.Main,
                    prerequisiteQuestId: "main_q4_serve_customer",
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_reach_pop_5", ObjectiveType.Population, null, 5, "Reach Population 5")
                    },
                    new QuestReward(300, 75)
                )
            },

            // Side Quests
            {
                "side_q1_farmer_request",
                new QuestDefinition(
                    "side_q1_farmer_request",
                    "Farmer's Request",
                    "Harvest a batch of fresh wheat for local storage.",
                    QuestType.Side,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_harvest_wheat_10", ObjectiveType.Harvest, "wheat", 10, "Harvest Wheat ×10")
                    },
                    new QuestReward(150, 30)
                )
            },
            {
                "side_q2_busy_bakery",
                new QuestDefinition(
                    "side_q2_busy_bakery",
                    "Busy Bakery",
                    "Bake fresh bread to supply the town market.",
                    QuestType.Side,
                    prerequisiteQuestId: null,
                    requiredLevel: 2,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_produce_bread_5", ObjectiveType.Produce, "recipe_bread", 5, "Produce Bread ×5")
                    },
                    new QuestReward(250, 50)
                )
            },

            // Daily Goals
            {
                "daily_g1_harvest_crops",
                new QuestDefinition(
                    "daily_g1_harvest_crops",
                    "Daily Harvest",
                    "Harvest 5 crops today.",
                    QuestType.Daily,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_daily_harvest_5", ObjectiveType.Harvest, null, 5, "Harvest 5 Crops")
                    },
                    new QuestReward(100, 20),
                    expirationDurationSeconds: 86400f
                )
            },
            {
                "daily_g2_fulfill_orders",
                new QuestDefinition(
                    "daily_g2_fulfill_orders",
                    "Daily Deliveries",
                    "Fulfill 2 customer orders today.",
                    QuestType.Daily,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_daily_deliver_2", ObjectiveType.Deliver, null, 2, "Fulfill 2 Orders")
                    },
                    new QuestReward(150, 30),
                    expirationDurationSeconds: 86400f
                )
            },

            // Milestones
            {
                "ms_first_harvest",
                new QuestDefinition(
                    "ms_first_harvest",
                    "First Harvest",
                    "Harvest your very first crop.",
                    QuestType.Milestone,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_ms_harvest_1", ObjectiveType.Harvest, null, 1, "Harvest 1 Crop")
                    },
                    new QuestReward(50, 10)
                )
            },
            {
                "ms_builder_10",
                new QuestDefinition(
                    "ms_builder_10",
                    "Town Builder",
                    "Construct 5 buildings in your town.",
                    QuestType.Milestone,
                    prerequisiteQuestId: null,
                    requiredLevel: 1,
                    new List<QuestObjectiveDefinition>
                    {
                        new QuestObjectiveDefinition("obj_ms_build_5", ObjectiveType.Build, null, 5, "Build 5 Buildings")
                    },
                    new QuestReward(300, 100)
                )
            }
        };

        public static QuestDefinition GetQuest(string questId)
        {
            return _quests.TryGetValue(questId, out var def) ? def : null;
        }

        public static List<QuestDefinition> GetAllQuests()
        {
            return new List<QuestDefinition>(_quests.Values);
        }

        public static List<QuestDefinition> GetQuestsByType(QuestType type)
        {
            var list = new List<QuestDefinition>();
            foreach (var q in _quests.Values)
            {
                if (q.Type == type) list.Add(q);
            }
            return list;
        }
    }
}
