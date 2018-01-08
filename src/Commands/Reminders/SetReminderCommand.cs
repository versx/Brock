namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Services;
    using BrockBot.Utilities;

    [Command(
        Categories.Reminders,
        "Have " + FilterBot.BotName + " remind you to do something.",
        "\tExample: `.remindme to update you in 2 minutes 30s`",
        "remindme"
    )]
    public class SetReminderCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public ReminderService ReminderSvc { get; }

        public SetReminderCommand(DiscordClient client, IDatabase db, ReminderService reminderSvc)
        {
            Client = client;
            Db = db;
            ReminderSvc = reminderSvc;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var reminder = command.Args[0];
            await SetReminder(message, reminder);
        }

        public async Task SetReminder(DiscordMessage message, string reminder, string seperator = "in")
        {
            try
            {
                var userId = message.Author.Id;
                var time = GetTimeInterval(reminder);
                if (Convert.ToInt32(time) == 0)
                {
                    await message.RespondAsync(":no_entry_sign: Invalid time format...");
                    return;
                }

                if (!CheckTimeInterval(userId, time))
                {
                    await message.RespondAsync(":no_entry_sign: You cannot have two reminders that are within 60 seconds of each other.");
                    return;
                }

                var reminders = new List<Reminder>();
                if (Db.Reminders.ContainsKey(userId))
                {
                    Db.Reminders.TryGetValue(userId, out reminders);
                }
                reminder = SanitizeString(reminder);

                var msg = reminder.Substring(0, reminder.LastIndexOf(seperator, StringComparison.Ordinal));
                var data = new Reminder
                {
                    Time = DateTime.UtcNow.AddSeconds(time),
                    Message = msg
                };
                reminders.Add(data);
                Db.Reminders.AddOrUpdate(userId, reminders, (key, oldValue) => reminders);
                ReminderSvc.ChangeToClosestInterval();
                Db.Save();
                await message.RespondAsync($":white_check_mark: Successfully set reminder. I will remind {message.Author.Username} to `{data.Message}` in `{reminder.Substring(reminder.LastIndexOf(seperator, StringComparison.Ordinal) + seperator.Length)}`!");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
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
                        Console.WriteLine("CAPTURES COUNT LESS THEN 3");
                        return 0;
                    }

                    if (!double.TryParse(captures[1].ToString(), out double amount))
                    {
                        Console.WriteLine($"COULD NOT PARSE DOUBLE : {captures[1].ToString()}");
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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return 0;
        }

        private bool CheckTimeInterval(ulong userId, double time)
        {
            try
            {
                if (!Db.Reminders.ContainsKey(userId))
                    return true;

                var reminders = new List<Reminder>();
                Db.Reminders.TryGetValue(userId, out reminders);

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
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }
    }
}