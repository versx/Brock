namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    [Command(
        Categories.General,
        "Shows this help message.",
        null,
        "help", "commands", "?"
    )]
    public class HelpCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public async Task Execute(DiscordMessage message, Command command) => await Task.CompletedTask;
    }
}
