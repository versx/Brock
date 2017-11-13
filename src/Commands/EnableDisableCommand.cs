namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using BrockBot.Data;

    public class EnableDisableCommand : ICustomCommand
    {
        private readonly Database _db;
        private readonly bool _enable;

        public bool AdminCommand => false;

        public EnableDisableCommand(Database db, bool enable)
        {
            _db = db;
            _enable = enable;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;
            if (!server.ContainsKey(author))
            {
                await message.RespondAsync("You currently do not have any Pokemon subscriptions.");
                return;
            }

            server[author].Enabled = _enable;
            await message.RespondAsync($"You have {(_enable ? "" : "de-")}activated Pokemon notifications.");
        }
    }
}