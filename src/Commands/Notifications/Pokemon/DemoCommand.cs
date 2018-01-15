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

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

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
            eb.WithTitle("Brock Pokemon Notification Demo Usage:");
            eb.AddField("Subscribes to Bulbasaur, Dratini, Dragonair, and Dragonite notifications with a minimum IV of 93% or higher.", "`.pokeme 1,147,148,149 93`");
            eb.AddField("Accidentally subscribed to Bulbasaur, unsubscribing...", "`.pokemenot 1`");
            eb.AddField("Activating the Pokemon notification subscriptions.", "`.enable`");
            eb.AddField("Displays your current Pokemon notification setting information.", "`.info`");
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}