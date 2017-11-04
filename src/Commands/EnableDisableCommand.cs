namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using PokeFilterBot.Data;

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
            var author = message.Author.Id;
            if (_db.Subscriptions.ContainsKey(author))
            {
                _db.Subscriptions[author].Enabled = _enable;
                await message.RespondAsync($"You have {(_enable ? "" : "de-")}activated Pokemon notifications.");
            }
        }
    }
}