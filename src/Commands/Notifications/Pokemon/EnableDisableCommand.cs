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
        private readonly bool _enable;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public EnableDisableCommand(DiscordClient client, IDatabase db, bool enable)
        {
            Client = client;
            Db = db;
            _enable = enable;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;
            if (!server.SubscriptionExists(author))
            {
                await message.RespondAsync($"{message.Author.Username} is not currently subscribed to any Pokemon or Raid notifications.");
                return;
            }

            server[author].Enabled = _enable;
            await message.RespondAsync($"{message.Author.Username} has **{(_enable ? "en" : "dis")}abled** Pokemon and Raid notifications.");
        }
    }
}