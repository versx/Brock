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
        "Activates or deactivates all of your Pokemon subscriptions at once.",
        null,
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
            if (!server.ContainsKey(author))
            {
                await message.RespondAsync("You currently do not have any Pokemon subscriptions.");
                return;
            }

            server[author].Enabled = _enable;
            await message.RespondAsync($"You have {(_enable ? "" : "de-")}activated Pokemon notifications.");
        }
    }
}