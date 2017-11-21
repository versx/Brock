namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command("map", "maps")]
    public class MapCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public MapCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return; //REVIEW: Should the command proceed even with unnecessary parameters?

            await message.RespondAsync($"Pokemon map: https://pokemap.ver.sx\r\nGyms & Raids map: https://gymmap.ver.sx");
        }
    }
}