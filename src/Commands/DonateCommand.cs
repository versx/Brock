namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command("donate")]
    public sealed class DonateCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; set; }

        public IDatabase Db { get; set; }

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                "PayPal:\r\nhttps://paypal.me/versx\r\n\r\n" +
                "Venmo:\r\nhttps://venmo.com/versx\r\n\r\n" +
                "Bitcoin:\r\n14KgVXUw3yTeb1rRdX5Dp45K2zvDebNo5B"
            );
        }
    }
}