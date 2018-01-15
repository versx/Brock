namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command(Categories.General,
        "Displays a list of available assignable city feed roles.",
        "\tExample: `.feeds`",
        "feeds", "cities"
    )]
    public class FeedsCommand : ICustomCommand
    {
        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public FeedsCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                $"**Available Assignable Cities:**\r\n{string.Join(Environment.NewLine, _config.CityRoles)}"
            );
        }
    }
}