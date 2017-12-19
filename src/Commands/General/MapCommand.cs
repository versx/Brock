namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.General,
        "Displays the Pokemon and Gyms & Raids map links.",
        "\tExample: `.map`\r\n" +
        "\tExample: `.maps`",
        "map", "maps"
    )]
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
            var eb = new DiscordEmbedBuilder();
            eb.AddField("Pokemon Map Scanner", "https://pokemap.ver.sx");
            eb.AddField("Gyms & Raids Map Scanner", "https://gymmap.ver.sx");
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}