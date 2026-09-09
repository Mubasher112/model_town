using System;
using System.Collections.Generic;

namespace Game.Platforms
{
    using Game.Services;

    public class LocalMockPlatformServices :
        IAuthenticationService,
        ICloudSaveService,
        IPushNotificationService,
        IInAppPurchaseService,
        IAdsService,
        IAnalyticsService,
        IAchievementService,
        ISocialService
    {
        public string PlatformName { get; }

        public LocalMockPlatformServices(string platformName)
        {
            PlatformName = platformName;
        }

        // IAuthenticationService
        public bool IsAuthenticated { get; private set; } = true;
        public string PlayerId { get; private set; } = "mock_player_123";
        public string DisplayName { get; private set; } = "Mock Player";

        public void Authenticate(Action<bool, string> callback)
        {
            IsAuthenticated = true;
            callback?.Invoke(true, "Authentication successful on " + PlatformName);
        }

        public void SignOut()
        {
            IsAuthenticated = false;
        }

        // ICloudSaveService
        public bool IsAvailable => true;
        private readonly Dictionary<string, string> _cloudStorage = new Dictionary<string, string>();

        public void SaveData(string key, string data, Action<bool, string> callback)
        {
            _cloudStorage[key] = data;
            callback?.Invoke(true, "Saved to " + PlatformName + " cloud");
        }

        public void LoadData(string key, Action<bool, string, string> callback)
        {
            if (_cloudStorage.TryGetValue(key, out var data))
            {
                callback?.Invoke(true, data, "Loaded from " + PlatformName + " cloud");
            }
            else
            {
                callback?.Invoke(false, null, "Key not found");
            }
        }

        // IPushNotificationService
        public void RequestPermission(Action<bool> callback) => callback?.Invoke(true);
        public void ScheduleNotification(string id, string title, string body, TimeSpan delay) { }
        public void CancelNotification(string id) { }
        public void CancelAllNotifications() { }

        // IInAppPurchaseService
        public bool IsInitialized => true;
        public void Initialize(Action<bool> callback) => callback?.Invoke(true);
        public void PurchaseProduct(string productId, Action<bool, string> callback) => callback?.Invoke(true, "Purchased " + productId);
        public void RestorePurchases(Action<bool, string> callback) => callback?.Invoke(true, "Purchases restored");

        // IAdsService
        public bool IsRewardedAdReady => true;
        public void ShowRewardedAd(Action<bool, string> callback) => callback?.Invoke(true, "Ad rewarded");
        public void ShowBannerAd() { }
        public void HideBannerAd() { }

        // IAnalyticsService
        public void LogEvent(string eventName, Dictionary<string, object> parameters = null) { }
        public void SetUserProperty(string key, string value) { }

        // IAchievementService
        public void UnlockAchievement(string achievementId, Action<bool, string> callback = null) => callback?.Invoke(true, "Unlocked " + achievementId);
        public void IncrementProgress(string achievementId, int steps, Action<bool, string> callback = null) => callback?.Invoke(true, "Progress updated");

        // ISocialService
        public void GetFriendsList(Action<bool, List<string>, string> callback) => callback?.Invoke(true, new List<string> { "Friend1", "Friend2" }, "Success");
        public void ShareContent(string message, Action<bool> callback = null) => callback?.Invoke(true);
    }

    public static class PlatformServiceFactory
    {
        public static LocalMockPlatformServices CreatePlatformServices()
        {
#if UNITY_ANDROID
            return new LocalMockPlatformServices("Android");
#elif UNITY_IOS
            return new LocalMockPlatformServices("iOS");
#else
            return new LocalMockPlatformServices("Editor/Desktop");
#endif
        }
    }
}
