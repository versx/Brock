namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command(Categories.General,
        "Displays all available nearby nests that the feed scans.",
        "\tExample: `.nests`",
        "nests"
    )]
    public class NearbyNestsCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public NearbyNestsCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (_config.NearbyNests == null || _config.NearbyNests.Count == 0)
            {
                await message.RespondAsync("No nearby nests are configured.");
                return;
            }

            await message.RespondAsync("Nests are out of date and a new way of updating the nests command is coming soon.");
            return;

            var msg = "**Nearby Nests**\r\n";
            foreach (var item in _config.NearbyNests)
            {
                if (_db.Pokemon.ContainsKey(item.Value.ToString()))
                {
                    msg += $"{item.Key}: {_db.Pokemon[item.Value.ToString()].Name}\r\n";
                }
            }

            await message.RespondAsync(msg);
        }
    }
}