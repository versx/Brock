namespace BrockBot.Services
{
    using System;

    public class NotificationLimiter
    {
        public const int MaxNotificationsPerMinute = 50;
        public const int ThresholdTimeout = 60;

        private TimeSpan _oneMinute;
        private DateTime _start;
        private DateTime _last;

        public int Count { get; private set; }

        public TimeSpan TimeLeft { get; private set; }

        public NotificationLimiter()
        {
            _oneMinute = TimeSpan.FromSeconds(0);
            _start = DateTime.Now;
            _last = DateTime.Now.Subtract(_oneMinute);

            TimeLeft = TimeSpan.MinValue;

            Reset();
        }

        public bool IsLimited()
        {
            TimeLeft = DateTime.Now.Subtract(_last);

            var sixtySeconds = TimeSpan.FromSeconds(ThresholdTimeout);
            var oneMinutePassed = TimeLeft >= sixtySeconds;
            if (oneMinutePassed)
            {
                Reset();
                _last = DateTime.Now.Subtract(_oneMinute);
            }

            if (Count >= MaxNotificationsPerMinute)
            {
                //Limited
                return true;
            }

            Count++;

            return false;
        }

        public void Reset()
        {
            Count = 0;
        }
    }

    //public class NotificationLimiter
    //{
    //    public const int MaxNotificationsPerMinute = 50;

    //    public long Count { get; set; }

    //    public DateTime LastNotification { get; set; }

    //    public NotificationLimiter()
    //    {
    //        Count = 0;
    //        LastNotification = DateTime.Now;
    //    }

    //    public bool IsLimited()
    //    {
    //        if (Count >= MaxNotificationsPerMinute)
    //        {
    //            return true;
    //        }

    //        if (LastNotification < DateTime.Now)
    //        {
    //            Count++;
    //            LastNotification = DateTime.Now;
    //            return false;
    //        }

    //        return true;
    //    }
    //}
}