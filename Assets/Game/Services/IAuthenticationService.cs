using System;

namespace Game.Services
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        string PlayerId { get; }
        string DisplayName { get; }
        void Authenticate(Action<bool, string> callback);
        void SignOut();
    }
}
