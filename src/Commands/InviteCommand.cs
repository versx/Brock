namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.General,
        "Returns an invite link to have " + FilterBot.BotName + " join a server you control.",
        null,
        "invite"
    )]
    public class InviteCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public InviteCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            await message.RespondAsync("https://discordapp.com/oauth2/authorize?&client_id=384254044690186255&scope=bot&permissions=0");
        }
    }
}