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

            Count = 0;
            TimeLeft = TimeSpan.MinValue;
        }

        public virtual bool IsLimited()
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

        public virtual void Reset()
        {
            Count = 0;
        }
    }
}