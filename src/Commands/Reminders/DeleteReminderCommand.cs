namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Utilities;

    [Command(
        Categories.Reminders,
        "Stop " + Strings.BotName + " from reminding you to do something based on the reminders index.",
        "\tExample: `.remindmenot 4`",
        "remindmenot"
    )]
    public class DeleteReminderCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DeleteReminderCommand(DiscordClient client, IDatabase db, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            if (!int.TryParse(command.Args[0], out int index))
            {
                await message.RespondAsync("Invalid message number specified, try again.");
                return;
            }

            await DeleteReminder(message, index);
        }

        public async Task DeleteReminder(DiscordMessage message, int index)
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

                var reminders = _db.Reminders[message.Author.Id];
                if (index > (reminders.Count + 1) || index < 1)
                {
                    await message.RespondAsync($"{message.Author.Mention} provided an invalid reminder number.");
                    return;
                }

                if (index == reminders.Count + 1)
                {
                    await message.RespondAsync($"Action cancelled by user {message.Author.Mention}.");
                    return;
                }

                index--;
                var msg = reminders[index].Message;
                reminders.RemoveAt(index);
                _db.Save();

                await message.RespondAsync($":white_check_mark: Successfully removed reminder '{msg}' for {message.Author.Mention}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}