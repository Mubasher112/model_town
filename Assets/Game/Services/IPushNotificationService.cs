using System;

namespace Game.Services
{
    public interface IPushNotificationService
    {
        void RequestPermission(Action<bool> callback);
        void ScheduleNotification(string id, string title, string body, TimeSpan delay);
        void CancelNotification(string id);
        void CancelAllNotifications();
    }
}
