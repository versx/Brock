namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.Reminders,
        "Stop " + FilterBot.BotName + " from reminding you to do something.",
        "\tExample: `.remindmenot `",
        "remindmenot"
    )]
    public class DeleteReminderCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public DeleteReminderCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;
            if (command.Args.Count != 1) return;

            if (!int.TryParse(command.Args[0], out int index))
            {
            }
            await DeleteReminder(message, index);
        }

        public async Task DeleteReminder(DiscordMessage message, int index)
        {
            try
            {
                if (!Db.Reminders.ContainsKey(message.Author.Id))
                {
                    await message.RespondAsync($":no_entry_sign: {message.Author.Username} does not have any reminders set.");
                    return;
                }

                if (Db.Reminders.Count < 1)
                {
                    await message.RespondAsync($":no_entry_sign: {message.Author.Username} does not have any reminders set.");
                    return;
                }

                //string msg = string.Empty;
                //for (int i = 0; i < Db.Reminders[message.Author.Id].Count; i++)
                //{
                //    msg += $"**{i + 1}:** {Db.Reminders[message.Author.Id][i].Message}\r\n";
                //}

                //msg += $"**{Db.Reminders[message.Author.Id].Count + 1}:** Cancel";
                //await message.RespondAsync(msg);

                if (index > (Db.Reminders[message.Author.Id].Count + 1) || index < 1)
                {
                    await message.RespondAsync($":no_entry_sign: {message.Author.Username} provided an invalid reminder number.");
                    return;
                }

                if (index == Db.Reminders[message.Author.Id].Count)
                {
                    await message.RespondAsync($":no_entry_sign: Action cancelled by user {message.Author.Username}.");
                    return;
                }

                index -= 1;
                var msgToRemove = Db.Reminders[message.Author.Id][index].Message;
                Db.Reminders[message.Author.Id].RemoveAt(index);
                Db.Save();

                await message.RespondAsync($":white_check_mark: Successfully removed reminder: '{msgToRemove}' for {message.Author.Username}.");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }
    }
}