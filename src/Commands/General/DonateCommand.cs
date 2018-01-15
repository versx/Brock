namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.General,
        "Displays information regarding how to donate.",
        "\tExample: `.donate`",
        "donate"
    )]
    public sealed class DonateCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; set; }

        public IDatabase Db { get; set; }

        public async Task Execute(DiscordMessage message, Command command)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("**Donation Information**");
            eb.AddField("PayPal", "https://paypal.me/versx");
            eb.AddField("Venmo", "https://venmo.com/versx");
            eb.AddField("Bitcoin", "14KgVXUw3yTeb1rRdX5Dp45K2zvDebNo5B");
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}