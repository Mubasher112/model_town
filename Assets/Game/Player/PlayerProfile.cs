using System;

namespace Game.Player
{
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerName = "Mayor";
        public int Level = 1;
        public int CurrentXP = 0;
        public long Coins = 500;
        public int Gems = 20;

        public int RequiredXPForNextLevel => Level * 100;

        [NonSerialized]
        public Action OnProfileUpdated;

        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            CurrentXP += amount;
            while (CurrentXP >= RequiredXPForNextLevel)
            {
                CurrentXP -= RequiredXPForNextLevel;
                Level++;
            }
            OnProfileUpdated?.Invoke();
        }

        public bool AddCoins(long amount)
        {
            if (amount < 0 && Coins < Math.Abs(amount)) return false;
            Coins += amount;
            OnProfileUpdated?.Invoke();
            return true;
        }

        public bool AddGems(int amount)
        {
            if (amount < 0 && Gems < Math.Abs(amount)) return false;
            Gems += amount;
            OnProfileUpdated?.Invoke();
            return true;
        }

        public void NotifyChanged()
        {
            OnProfileUpdated?.Invoke();
        }
    }
}
