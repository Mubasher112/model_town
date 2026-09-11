using System;

namespace Game.Services
{
    public interface IAdsService
    {
        bool IsInitialized { get; }
        void Initialize(Action<bool> callback);
        bool IsRewardedAdReady { get; }
        void ShowRewardedAd(Action<bool, string> callback);
        void ShowBannerAd();
        void HideBannerAd();
    }
}
