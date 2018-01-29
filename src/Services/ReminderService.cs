namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using DSharpPlus;

    using Newtonsoft.Json;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    public class ReminderService
    {
        #region Constants

        private const int INITIAL_DELAY = 40;
        private const ushort MAX_REMINDERS = 10;

        #endregion

        #region Variables

        private Timer _timer;
        private readonly Database _db;
        private readonly DiscordClient _client;
        private readonly IEventLogger _logger;

        #endregion

        #region Constructor

        public ReminderService(DiscordClient client, Database db, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _logger = logger;

            InitializeTimer();
        }

        #endregion

        #region Private Methods

        private void InitializeTimer()
        {
            try
            {
#pragma warning disable RECS0165
                _timer = new Timer(async x =>
#pragma warning restore RECS0165
                {
                    foreach (var user in _db.Reminders)
                    {
                        var itemsToRemove = new List<Reminder>();
                        foreach (var reminder in user.Value)
                        {
                            _logger.Info($"Reminder for user {user.Key} {DateTime.UtcNow.Subtract(reminder.Time)} time left.");
                            if (reminder.Time.CompareTo(DateTime.UtcNow) <= 0)
                            {
                                var userToRemind = await _client.GetUser(user.Key);
                                if (userToRemind == null)
                                {
                                    _logger.Error($"Failed to find discord user with id '{user.Key}'.");
                                    continue;
                                }

                                _logger.Info($"NOTIFYING USER OF REMINDER: {reminder.Message}");
                                if (reminder.Where == 0)
                                {
                                    await _client.SendDirectMessage(userToRemind, $":alarm_clock: **Reminder:** {reminder.Message}", null);
                                }
                                else
                                {
                                    var channel = await _client.GetChannel(reminder.Where);
                                    if (channel == null)
                                    {
                                        _logger.Error($"Failed to find channel {reminder.Where} that reminder should be sent to, sending via DM instead...");
                                        await _client.SendDirectMessage(userToRemind, $":alarm_clock: **Reminder:** {reminder.Message}", null);
                                    }
                                    else
                                    {
                                        await channel.SendMessageAsync($":alarm_clock: {userToRemind.Mention} **Reminder:** {reminder.Message}");
                                    }
                                }
                                itemsToRemove.Add(reminder);
                            }
                        }

                        foreach (var remove in itemsToRemove)
                        {
                            user.Value.Remove(remove);
                        }
                        _db.Reminders.TryUpdate(user.Key, user.Value, user.Value);
                    }

                    ChangeToClosestInterval();
                    _db.Save();
                },
                null,
                TimeSpan.FromSeconds(INITIAL_DELAY),// Time that message should fire after bot has started
                TimeSpan.FromSeconds(INITIAL_DELAY)); //time after which message should repeat (timout.infinite for no repeat)
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }

        public void ChangeToClosestInterval()
        {
            var timeToUpdate = double.PositiveInfinity;
            foreach (var user in _db.Reminders)
            {
                foreach (var reminder in user.Value)
                {
                    var delta = reminder.Time.Subtract(DateTime.UtcNow).TotalSeconds;
                    if (delta < 0)
                        delta = 0;
                    if (timeToUpdate > delta)
                        timeToUpdate = delta;
                }
            }

            if (double.IsPositiveInfinity(timeToUpdate))
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
                Console.WriteLine($"TIMER HAS BEEN HALTED!");
            }
            else
            {
                _timer.Change(TimeSpan.FromSeconds(timeToUpdate), TimeSpan.FromSeconds(timeToUpdate));
                Console.WriteLine($"CHANGED TIMER INTERVAL TO: {timeToUpdate}");
            }
        }

        public string ConvertTime(double value)
        {
            var ts = TimeSpan.FromSeconds(value);
            if (value > 86400)
                return string.Format("{0}d {1}h {2}m {3:D2}s", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);

            if (value > 3600)
                return string.Format("{0}h {1}m {2:D2}s", ts.Hours, ts.Minutes, ts.Seconds);

            if (value > 60)
                return string.Format("{0}m {1:D2}s", ts.Minutes, ts.Seconds);

            return string.Format("{0:D2}s", ts.Seconds);
        }

        #endregion
    }

    [JsonObject("reminder")]
    public class Reminder
    {
        [JsonProperty("time")]
        public DateTime Time { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("where")]
        public ulong Where { get; set; }
    }
}