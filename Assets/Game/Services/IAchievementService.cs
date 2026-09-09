using System;

namespace Game.Services
{
    public interface IAchievementService
    {
        void UnlockAchievement(string achievementId, Action<bool, string> callback = null);
        void IncrementProgress(string achievementId, int steps, Action<bool, string> callback = null);
    }
}
