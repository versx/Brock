namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command("demo")]
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
            await message.RespondAsync
            (
                $"Below is a demo of how to operate {AssemblyUtils.AssemblyName}:\r\n" +
                "We will setup channel subscriptions from #upland_rares and #upland_ultra\r\n" +
                "`.add upland_rares,upland_ultra`\r\n\r\n" +
                "Subscribes to Bulbasaur, Dratini, Dragonair, and Dragonite Pokemon notifications.\r\n" +
                "`.sub 1,147,148,149`\r\n\r\n" +
                "Accidentally subscribed to Bulbasaur, unsubscribing...\r\n" +
                "`.unsub 1`\r\n\r\n" +
                "Accidentally setup channel subscriptions for #upland_rares, removing...\r\n" +
                "`.remove upland_rares`\r\n\r\n" +
                "Activating the notification subscriptions.\r\n" +
                "`.enable`\r\n\r\n" +
                "Displays your current notification information.\r\n" +
                "`.info`"
            );
        }
    }
}