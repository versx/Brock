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
        "",
        //CommandPermissionLevel.User,
        "nests"
    )]
    public class NearbyNestsCommand : ICustomCommand
    {
        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public NearbyNestsCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
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

            var msg = "**Nearby Nests**\r\n";
            foreach (var item in _config.NearbyNests)
            {
                msg += $"{item.Key}: {Db.Pokemon[item.Value.ToString()].Name}\r\n";
            }

            await message.RespondAsync(msg);
        }
    }
}