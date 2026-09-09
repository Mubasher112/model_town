using System;
using System.Collections.Generic;

namespace Game.Services
{
    public interface ISocialService
    {
        void GetFriendsList(Action<bool, List<string>, string> callback);
        void ShareContent(string message, Action<bool> callback = null);
    }
}
