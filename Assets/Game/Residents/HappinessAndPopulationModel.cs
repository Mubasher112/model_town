using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Data;
using Game.Buildings;

namespace Game.Residents
{
    [Serializable]
    public class ResidentInstance
    {
        public string ResidentId;
        public string ResidentTypeId;
        public string DisplayName;
        public string AssignedHouseInstanceId;
        public ResidentState State = ResidentState.Unassigned;
        public long CreationUtcTicks;

        public ResidentInstance() { }

        public ResidentInstance(string residentId, string residentTypeId, string displayName, long creationUtcTicks)
        {
            ResidentId = residentId;
            ResidentTypeId = residentTypeId;
            DisplayName = displayName;
            AssignedHouseInstanceId = null;
            State = ResidentState.Unassigned;
            CreationUtcTicks = creationUtcTicks;
        }
    }

    public class HappinessModifier
    {
        public string ModifierId { get; set; }
        public string Description { get; set; }
        public int ScoreDelta { get; set; }

        public HappinessModifier(string modifierId, string description, int scoreDelta)
        {
            ModifierId = modifierId;
            Description = description;
            ScoreDelta = scoreDelta;
        }
    }

    public class HappinessManager
    {
        public const int BaseHappinessScore = 50;

        public int CurrentHappinessScore { get; private set; } = 50;
        public string HappinessRating { get; private set; } = "Good";

        private readonly List<HappinessModifier> _modifiers = new List<HappinessModifier>();

        public event Action<int, string> OnHappinessChanged;

        public void RecalculateHappiness(
            List<BuildingInstance> buildings,
            int currentPopulation,
            int totalHousingCapacity)
        {
            _modifiers.Clear();

            int buildingBonus = 0;
            if (buildings != null)
            {
                foreach (var b in buildings)
                {
                    if (b.State == BuildingState.Completed)
                    {
                        var config = BuildingLibrary.GetBuilding(b.BuildingId);
                        if (config != null && config.HappinessBonus > 0)
                        {
                            buildingBonus += config.HappinessBonus * b.Level;
                        }
                    }
                }
            }

            if (buildingBonus > 0)
            {
                _modifiers.Add(new HappinessModifier("bldg_bonus", "Town Facilities & Decorations", buildingBonus));
            }

            int unhousedCount = Math.Max(0, currentPopulation - totalHousingCapacity);
            if (unhousedCount > 0)
            {
                int penalty = unhousedCount * 10;
                _modifiers.Add(new HappinessModifier("housing_shortage", $"Housing Shortage ({unhousedCount} unhoused)", -penalty));
            }

            int score = BaseHappinessScore;
            foreach (var mod in _modifiers)
            {
                score += mod.ScoreDelta;
            }

            CurrentHappinessScore = Math.Clamp(score, 0, 100);
            HappinessRating = GetRatingForScore(CurrentHappinessScore);

            OnHappinessChanged?.Invoke(CurrentHappinessScore, HappinessRating);
        }

        public void SetOverrideScore(int score)
        {
            CurrentHappinessScore = Math.Clamp(score, 0, 100);
            HappinessRating = GetRatingForScore(CurrentHappinessScore);
            OnHappinessChanged?.Invoke(CurrentHappinessScore, HappinessRating);
        }

        public static string GetRatingForScore(int score)
        {
            if (score >= 80) return "Excellent";
            if (score >= 60) return "Good";
            if (score >= 40) return "Average";
            if (score >= 20) return "Low";
            return "Critical";
        }

        public List<HappinessModifier> GetActiveModifiers()
        {
            return new List<HappinessModifier>(_modifiers);
        }
    }

    public class PopulationStats
    {
        public int CurrentPopulation;
        public int TotalHousingCapacity;
        public int AvailableHousingSlots;
        public int ResidentialHousesCount;
        public int OccupiedHousesCount;
        public int UnassignedResidentsCount;
        public int HappinessScore;
        public string HappinessRating;
    }
}
