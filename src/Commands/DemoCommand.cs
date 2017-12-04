namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.Notifications,
        "Demos how to use " + FilterBot.BotName + ".",
        null,
        "demo"
    )]
    public class DemoCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public DemoCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Brock Demo Usage:");
            eb.AddField("We will setup channel subscriptions from #upland_rares and #upland_ultra", "`.add upland_rares,upland_ultra`");
            eb.AddField("Subscribes to Bulbasaur, Dratini, Dragonair, and Dragonite notifications.", "`.sub 1,147,148,149`");
            eb.AddField("Accidentally subscribed to Bulbasaur, unsubscribing...", "`.unsub 1`");
            eb.AddField("Accidentally setup channel subscriptions for #upland_rares, removing...", "`remove upland_rares`");
            eb.AddField("Activating the Pokemon notification subscriptions.", "`.enable`");
            eb.AddField("Displays your current Pokemon notification setting information.", "`.info`");
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}