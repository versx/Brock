namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.Administrative,
        "Shows all current bans for the guild.",
        null,
        "bans"
    )]
    public class BansCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (message.Channel == null)
            {
                await message.RespondAsync("DM is not supported yet for this command.");
                return;
            }

            var bans = await message.Channel.Guild.GetBansAsync();
            var msg = "**Bans**\r\n";
            foreach (var ban in bans)
            {
                msg += $"{ban.User.Username} ({ban.User.Id}): {ban.Reason}\r\n";
            }
            if (bans.Count == 0) msg += "Noone has been banned...yet!";

            await message.RespondAsync(msg);
        }
    }
}
