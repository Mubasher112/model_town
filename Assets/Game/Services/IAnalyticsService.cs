using System;
using System.Collections.Generic;

namespace Game.Services
{
    public interface IAnalyticsService
    {
        void LogEvent(string eventName, Dictionary<string, object> parameters = null);
        void SetUserProperty(string key, string value);
    }
}
