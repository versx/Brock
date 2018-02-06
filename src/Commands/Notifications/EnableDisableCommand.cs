namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Notifications,
        "Enables or disables all of your Pokemon and Raid notification subscriptions at once.",
        "\tExample: `.enable` (Enables all of your Pokemon and Raid notifications.)\r\n" +
        "\tExample: `.disable` (Disables all of your Pokemon and Raid notifications.)",
        "enable", "disable"
    )]
    public class EnableDisableCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public EnableDisableCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            var author = message.Author.Id;
            if (!_db.Exists(author))
            {
                await message.RespondAsync($"{message.Author.Mention} is not currently subscribed to any Pokemon or Raid notifications.");
                return;
            }

            _db[author].Enabled = string.Compare(command.Name, "enable", true) == 0;
            _db.Save();

            await message.RespondAsync($"{message.Author.Mention} has **{command.Name}d** Pokemon and Raid notifications.");
        }
    }
}