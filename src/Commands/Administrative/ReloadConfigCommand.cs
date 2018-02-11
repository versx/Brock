namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

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
        public Config Config { get; set; }

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public ReloadConfigCommand(Config config)
        {
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            Config = Config.Load();

            await Task.CompletedTask;
        }
    }
}