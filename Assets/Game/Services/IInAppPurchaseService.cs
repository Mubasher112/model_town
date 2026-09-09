using System;

namespace Game.Services
{
    public interface IInAppPurchaseService
    {
        bool IsInitialized { get; }
        void Initialize(Action<bool> callback);
        void PurchaseProduct(string productId, Action<bool, string> callback);
        void RestorePurchases(Action<bool, string> callback);
    }
}
