using System;
using Game.Player;

namespace Game.Economy
{
    public class EconomyManager
    {
        private readonly PlayerProfile _profile;

        public EconomyManager(PlayerProfile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public bool CanAffordCoins(long cost)
        {
            return cost >= 0 && _profile.Coins >= cost;
        }

        public bool CanAffordGems(int cost)
        {
            return cost >= 0 && _profile.Gems >= cost;
        }

        public bool SpendCoins(long amount)
        {
            if (amount < 0) return false;
            if (!CanAffordCoins(amount)) return false;
            return _profile.AddCoins(-amount);
        }

        public bool SpendGems(int amount)
        {
            if (amount < 0) return false;
            if (!CanAffordGems(amount)) return false;
            return _profile.AddGems(-amount);
        }

        public void EarnCoins(long amount)
        {
            if (amount <= 0) return;
            _profile.AddCoins(amount);
        }

        public void EarnGems(int amount)
        {
            if (amount <= 0) return;
            _profile.AddGems(amount);
        }

        public void AwardXP(int amount)
        {
            if (amount <= 0) return;
            _profile.AddXP(amount);
        }
    }
}
