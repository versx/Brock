namespace BrockBot.Services
{
    using System;

    public class NotificationLimiter
    {
        public const int MaxNotificationsPerMinute = 50;

        public long Count { get; set; }

        public DateTime LastNotification { get; set; }

        public NotificationLimiter()
        {
            Count = 0;
            LastNotification = DateTime.Now;
        }

        public bool IsLimited()
        {
            if (Count >= MaxNotificationsPerMinute)
            {
                return true;
            }

            if (LastNotification < DateTime.Now)
            {
                Count++;
                LastNotification = DateTime.Now;
                return false;
            }

            return true;
        }
    }
}