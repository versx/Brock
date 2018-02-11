namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;

    [Command(
        Categories.Notifications,
        "Reloads Brocks configuration file without having to restart him.",
        "\tExample: `.reload-config`",
        "reload-config"
    )]
    public class ReloadConfigCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public ReloadConfigCommand(DiscordClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            _config = Config.Load();

            await message.RespondAsync($"{message.Author.Mention} configuration file reloaded.");
        }
    }
}