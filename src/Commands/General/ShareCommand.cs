namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    
    using BrockBot.Diagnostics;

    [Command(
        Categories.General,
        "",
        "",
        "share"
    )]
    public class ShareCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public ShareCommand(DiscordClient client, IEventLogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            await Task.CompletedTask;
        }
    }
}