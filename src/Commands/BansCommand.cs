namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(
        Categories.General,
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
            await message.IsDirectMessageSupported();

            try
            {
                var bans = await message.Channel.Guild.GetBansAsync();
                var msg = $"**{message.Channel.Guild.Name}'s Server Bans:**\r\n";
                foreach (var ban in bans)
                {
                    msg += $"{ban.User.Username} ({ban.User.Id}): {ban.Reason}\r\n";
                }
                if (bans.Count == 0) msg += "No one has been banned...yet!";

                await message.RespondAsync(msg);
            }
            catch (Exception ex)
            {
                await message.RespondAsync("It appears that I do not have the correct permissions to perform that command.");
                Utils.LogError(ex);
            }
        }
    }
}
