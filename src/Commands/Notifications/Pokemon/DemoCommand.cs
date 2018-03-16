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
        "Demos how to use " + Strings.BotName + ".",
        null,
        "demo"
    )]
    public class DemoCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public DemoCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Brock Pokemon Notification Demo Usage:");
            eb.AddField("Subscribe to Bulbasaur, Dratini, Dragonair, and Dragonite notifications with a minimum IV of 93% or higher.", "`.pokeme 1,147,148,149 93`");
            eb.AddField("Unsubscribe from Bulbasaur", "`.pokemenot 1`");
            eb.AddField("Activating the Pokemon notification subscriptions.", "`.enable`");
            eb.AddField("Display your current Pokemon and Raid notification settings.", "`.info`");
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}