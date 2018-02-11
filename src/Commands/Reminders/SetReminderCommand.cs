namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Services;

    [Command(
        Categories.Reminders,
        "Have " + FilterBot.BotName + " remind you to do something.",
        "\tExample: `.remindme to update you in 2 minutes 30s` (Direct message)\r\n" +
        "\tExample: `.remindme here \"to update you in 2 minutes 30s\"` (In the current channel.)",
        "remindme"
    )]
    public class SetReminderCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public ReminderService ReminderSvc { get; }

        public SetReminderCommand(DiscordClient client, IDatabase db, ReminderService reminderSvc, IEventLogger logger)
        {
            _client = client;
            _db = db;
            ReminderSvc = reminderSvc;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            //if (command.Args.Count != 1) return;

            switch (command.Args.Count)
            {
                case 1:
                    await SetReminder(message, command.Args[0]);
                    break;
                case 2:
                    var where = command.Args[1];
                    await SetReminder(message, command.Args[1], command.Args[0]);
                    break;
            }
        }

        public async Task SetReminder(DiscordMessage message, string reminder, string where = "", string seperator = "in")
        {
            try
            {
                var userId = message.Author.Id;
                var time = GetTimeInterval(reminder);
                if (Convert.ToInt32(time) == 0)
                {
                    await message.RespondAsync($"{message.Author.Mention} you specified an invalid time format.");
                    return;
                }

                if (!CheckTimeInterval(userId, time))
                {
                    await message.RespondAsync($"{message.Author.Mention}, you cannot have two reminders that are within 60 seconds of each other.");
                    return;
                }

                ulong channelId = 0;
                if (!string.IsNullOrEmpty(where))
                {
                    channelId = message.ChannelId;
                }

                var reminders = new List<Reminder>();
                if (_db.Reminders.ContainsKey(userId))
                {
                    _db.Reminders.TryGetValue(userId, out reminders);
                }
                reminder = SanitizeString(reminder);

                var msg = reminder.Substring(0, reminder.LastIndexOf(seperator, StringComparison.Ordinal));
                var data = new Reminder
                {
                    Time = DateTime.UtcNow.AddSeconds(time),
                    Message = msg,
                    Where = channelId
                };
                reminders.Add(data);
                _db.Reminders.AddOrUpdate(userId, reminders, (key, oldValue) => reminders);
                ReminderSvc.ChangeToClosestInterval();
                _db.Save();

                await message.RespondAsync($"Successfully set reminder for {message.Author.Mention} to `{data.Message}` in `{reminder.Substring(reminder.LastIndexOf(seperator, StringComparison.Ordinal) + seperator.Length)}`!");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private string SanitizeString(string command)
        {
            command = command.Replace("mins", "m");
            command = command.Replace("minutes", "m");
            command = command.Replace("minute", "m");
            command = command.Replace("min", "m");
            return command;
        }

        private double GetTimeInterval(string message, string seperator = "in")
        {
            try
            {
                if (!message.Contains(seperator)) return 0;

                message = SanitizeString(message);

                var msg = message.Substring(message.LastIndexOf(seperator, StringComparison.Ordinal) + seperator.Length);
                var regex = Regex.Matches(msg, @"(\d+)\s{0,1}([a-zA-Z]*)");

                var timeToAdd = 0d;
                for (int i = 0; i < regex.Count; i++)
                {
                    var captures = regex[i].Groups;
                    if (captures.Count < 3)
                    {
                        _logger.Error("Captures count is less than 3");
                        return 0;
                    }

                    if (!double.TryParse(captures[1].ToString(), out double amount))
                    {
                        _logger.Error($"Failed to parse double: {captures[1].ToString()}");
                        return 0;
                    }

                    switch (captures[2].ToString())
                    {
                        case "weeks":
                        case "week":
                        case "w":
                            timeToAdd += amount * 604800;
                            break;
                        case "days":
                        case "day":
                        case "d":
                            timeToAdd += amount * 86400;
                            break;
                        case "hours":
                        case "hour":
                        case "h":
                            timeToAdd += amount * 3600;
                            break;
                        case "minutes":
                        case "minute":
                        case "mins":
                        case "min":
                        case "m":
                            timeToAdd += amount * 60;
                            break;
                        case "seconds":
                        case "second":
                        case "s":
                            timeToAdd += amount;
                            break;
                        default:
                            return 0;
                    }
                }
                return timeToAdd;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return 0;
        }

        private bool CheckTimeInterval(ulong userId, double time)
        {
            try
            {
                if (!_db.Reminders.ContainsKey(userId))
                    return true;

                var reminders = new List<Reminder>();
                _db.Reminders.TryGetValue(userId, out reminders);

                var closestTime = double.PositiveInfinity;
                var dateTimeToFind = DateTime.UtcNow.AddSeconds(time);

                foreach (var reminder in reminders)
                {
                    var delta = Math.Abs(reminder.Time.Subtract(dateTimeToFind).TotalSeconds);
                    if (delta < 0) delta = 0;
                    if (closestTime > delta) closestTime = delta;
                }
                if (closestTime <= 60) return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            return false;
        }
    }
}