namespace BrockBot.Commands
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    
    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Services;

    [Command(
        Categories.Reminders,
        "List all reminders that " + FilterBot.BotName + " should notify you of.",
        "\tExample: `.reminders`",
        "reminders"
    )]
    public class GetRemindersCommand : ICustomCommand
    {
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public ReminderService ReminderSvc { get; }

        public GetRemindersCommand(DiscordClient client, IDatabase db, ReminderService reminderSvc, IEventLogger logger)
        {
            Client = client;
            Db = db;
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
                if (!Db.Reminders.ContainsKey(message.Author.Id))
                {
                    await message.RespondAsync($":no_entry_sign: {message.Author.Mention} does not have any reminders set.");
                    return;
                }

                if (Db.Reminders.Count < 1)
                {
                    await message.RespondAsync($":no_entry_sign: {message.Author.Mention} does not have any reminders set.");
                    return;
                }

                var orderedReminders = Db.Reminders[message.Author.Id].OrderBy(x => x.Time).ToList();

                var eb = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor(4, 97, 247),
                    ThumbnailUrl = message.Author.AvatarUrl,
                    Title = $"{message.Author.Mention}, your reminders are the following:"
                };

                for (int i = 0; i < orderedReminders.Count && i < 10; i++)
                {
                    var msg = $"Reminder #{i + 1} in {ReminderSvc.ConvertTime(orderedReminders[i].Time.Subtract(DateTime.UtcNow).TotalSeconds)}";
                    _logger.Debug($"{msg}: {orderedReminders[i].Message}");

                    eb.AddField(msg, $"{orderedReminders[i].Message}");
                }

                var embed = eb.Build();
                if (embed == null) return;

                await message.RespondAsync(string.Empty, false, embed);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}