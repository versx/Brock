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
        "Stop " + FilterBot.BotName + " from reminding you to do something.",
        "\tExample: `.remindmenot `",
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

                //string msg = string.Empty;
                //for (int i = 0; i < Db.Reminders[message.Author.Id].Count; i++)
                //{
                //    msg += $"**{i + 1}:** {Db.Reminders[message.Author.Id][i].Message}\r\n";
                //}

                //msg += $"**{Db.Reminders[message.Author.Id].Count + 1}:** Cancel";
                //await message.RespondAsync(msg);

                if (index > (_db.Reminders[message.Author.Id].Count + 1) || index < 1)
                {
                    await message.RespondAsync($"{message.Author.Mention} provided an invalid reminder number.");
                    return;
                }

                if (index == _db.Reminders[message.Author.Id].Count)
                {
                    await message.RespondAsync($"Action cancelled by user {message.Author.Mention}.");
                    return;
                }

                index -= 1;
                var msgToRemove = _db.Reminders[message.Author.Id][index].Message;
                _db.Reminders[message.Author.Id].RemoveAt(index);
                _db.Save();

                await message.RespondAsync($":white_check_mark: Successfully removed reminder: '{msgToRemove}' for {message.Author.Mention}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}