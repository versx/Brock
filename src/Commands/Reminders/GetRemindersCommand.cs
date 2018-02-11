namespace BrockBot.Commands
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    
    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Services;

    [Command(
        Categories.Reminders,
        "List all reminders that " + FilterBot.BotName + " will notify you of.",
        "\tExample: `.reminders`",
        "reminders"
    )]
    public class GetRemindersCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public ReminderService ReminderSvc { get; }

        public GetRemindersCommand(DiscordClient client, IDatabase db, ReminderService reminderSvc, IEventLogger logger)
        {
            _client = client;
            _db = db;
            ReminderSvc = reminderSvc;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            await GetReminders(message);
        }

        public async Task GetReminders(DiscordMessage message)
        {
            try
            {
                if (!_db.Reminders.ContainsKey(message.Author.Id))
                {
                    await message.RespondAsync($"{message.Author.Mention} does not have any reminders set.");
                    return;
                }

                if (_db.Reminders.Count == 0)
                {
                    await message.RespondAsync($"{message.Author.Mention} does not have any reminders set.");
                    return;
                }

                var orderedReminders = _db.Reminders[message.Author.Id].OrderBy(x => x.Time).ToList();

                var eb = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(4, 97, 247),
                    ThumbnailUrl = message.Author.AvatarUrl,
                    Title = $"{message.Author.Username}, your reminders are the following:"
                };

                for (int i = 0; i < orderedReminders.Count && i < 10; i++)
                {
                    var msg = $"Reminder #{i + 1} in {ReminderSvc.ConvertTime(orderedReminders[i].Time.Subtract(DateTime.UtcNow).TotalSeconds)}";
                    _logger.Debug($"{msg}: {orderedReminders[i].Message}");

                    var channel = orderedReminders[i].Where == 0 ? null : await _client.GetChannel(orderedReminders[i].Where);
                    eb.AddField(msg, $"{(channel?.Name ?? "DM")}: {orderedReminders[i].Message}");
                }

                var embed = eb.Build();
                if (embed == null) return;

                await message.RespondAsync(message.Author.Mention, false, embed);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}