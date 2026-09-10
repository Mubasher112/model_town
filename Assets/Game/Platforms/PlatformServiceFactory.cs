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

        // ISocialService delegate to MockSocialService
        private readonly MockSocialService _mockSocial = new MockSocialService();

        public bool IsOnline => _mockSocial.IsOnline;
        public void SetOnline(bool isOnline) => _mockSocial.SetOnline(isOnline);
        public void GetMyProfile(Action<bool, Game.Data.SocialProfile, string> callback) => _mockSocial.GetMyProfile(callback);
        public void UpdateProfile(string displayName, string avatarId, Action<bool, string> callback) => _mockSocial.UpdateProfile(displayName, avatarId, callback);
        public void SearchPlayers(string query, Action<bool, List<Game.Data.SocialProfile>, string> callback) => _mockSocial.SearchPlayers(query, callback);
        public void GetPlayerProfile(string targetPlayerId, Action<bool, Game.Data.SocialProfile, string> callback) => _mockSocial.GetPlayerProfile(targetPlayerId, callback);
        public void GetFriendsList(Action<bool, List<Game.Data.FriendRelationship>, string> callback) => _mockSocial.GetFriendsList(callback);
        public void SendFriendRequest(string targetPlayerId, Action<bool, string> callback) => _mockSocial.SendFriendRequest(targetPlayerId, callback);
        public void AcceptFriendRequest(string targetPlayerId, Action<bool, string> callback) => _mockSocial.AcceptFriendRequest(targetPlayerId, callback);
        public void RejectFriendRequest(string targetPlayerId, Action<bool, string> callback) => _mockSocial.RejectFriendRequest(targetPlayerId, callback);
        public void RemoveFriend(string targetPlayerId, Action<bool, string> callback) => _mockSocial.RemoveFriend(targetPlayerId, callback);
        public void VisitTown(string targetPlayerId, Action<bool, Game.Data.TownSnapshot, string> callback) => _mockSocial.VisitTown(targetPlayerId, callback);
        public void AppreciateTown(string targetPlayerId, Action<bool, string> callback) => _mockSocial.AppreciateTown(targetPlayerId, callback);
        public void SendGift(string targetPlayerId, string itemId, int quantity, Action<bool, string> callback) => _mockSocial.SendGift(targetPlayerId, itemId, quantity, callback);
        public void GetPendingGifts(Action<bool, List<Game.Data.SocialGift>, string> callback) => _mockSocial.GetPendingGifts(callback);
        public void ClaimGift(string giftId, Action<bool, string> callback) => _mockSocial.ClaimGift(giftId, callback);
        public void BlockPlayer(string targetPlayerId, Action<bool, string> callback) => _mockSocial.BlockPlayer(targetPlayerId, callback);
        public void UnblockPlayer(string targetPlayerId, Action<bool, string> callback) => _mockSocial.UnblockPlayer(targetPlayerId, callback);
        public void GetBlockedPlayers(Action<bool, List<string>, string> callback) => _mockSocial.GetBlockedPlayers(callback);
        public void ReportPlayer(string targetPlayerId, string reason, string description, Action<bool, string> callback) => _mockSocial.ReportPlayer(targetPlayerId, reason, description, callback);
        public void ShareContent(string message, Action<bool> callback = null) => _mockSocial.ShareContent(message, callback);
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
