namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.Administrative,
        "Kicks the specified discord user.",
        null,
        "kick"
    )]
    public class KickCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public KickCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await Client.Guilds[0].LeaveAsync();
            var guild = message.Channel.Guild;

            var member = await guild.GetMemberAsync(0);

            //var pruneCount = await guild.GetPruneCountAsync(365);
            //await guild.PruneAsync(365, "Inactive users");
        }
    }
}