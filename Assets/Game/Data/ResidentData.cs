using System;
using System.Collections.Generic;

namespace Game.Data
{
    public enum ResidentState
    {
        Unassigned,
        AtHome,
        Walking,
        Working,
        Shopping
    }

    [Serializable]
    public class ResidentDefinition
    {
        public string ResidentTypeId;
        public string DisplayName;
        public string Title;
        public int DefaultHappinessContribution = 5;
        public int UnlockLevelRequirement = 1;

        public ResidentDefinition() { }

        public ResidentDefinition(string residentTypeId, string displayName, string title, int defaultHappinessContribution = 5, int unlockLevelRequirement = 1)
        {
            ResidentTypeId = residentTypeId;
            DisplayName = displayName;
            Title = title;
            DefaultHappinessContribution = defaultHappinessContribution;
            UnlockLevelRequirement = unlockLevelRequirement;
        }
    }

    public static class ResidentLibrary
    {
        private static readonly List<ResidentDefinition> _definitions = new List<ResidentDefinition>
        {
            new ResidentDefinition("res_farmer", "Emma", "Farmer", defaultHappinessContribution: 5, unlockLevelRequirement: 1),
            new ResidentDefinition("res_baker", "John", "Baker", defaultHappinessContribution: 5, unlockLevelRequirement: 1),
            new ResidentDefinition("res_shopkeeper", "Maya", "Shopkeeper", defaultHappinessContribution: 6, unlockLevelRequirement: 2),
            new ResidentDefinition("res_builder", "Bob", "Builder", defaultHappinessContribution: 6, unlockLevelRequirement: 2),
            new ResidentDefinition("res_citizen", "Alex", "Resident", defaultHappinessContribution: 4, unlockLevelRequirement: 1)
        };

        public static ResidentDefinition GetDefinition(string typeId)
        {
            foreach (var d in _definitions)
            {
                if (d.ResidentTypeId == typeId) return d;
            }
            return _definitions[0];
        }

        public static ResidentDefinition GetRandomDefinitionForLevel(int playerLevel)
        {
            var eligible = new List<ResidentDefinition>();
            foreach (var d in _definitions)
            {
                if (d.UnlockLevelRequirement <= playerLevel)
                {
                    eligible.Add(d);
                }
            }

            if (eligible.Count == 0) return _definitions[0];
            var rand = new Random();
            return eligible[rand.Next(eligible.Count)];
        }

        public static List<ResidentDefinition> GetAllDefinitions()
        {
            return new List<ResidentDefinition>(_definitions);
        }
    }
}
