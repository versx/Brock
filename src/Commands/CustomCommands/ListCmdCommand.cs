namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command(
        Categories.CustomCommands,
        "Lists all available custom commands registered in the database.",
        "\tExample: .listcmd",
        "listcmd", "listcmds"
    )]
    public class ListCmdCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public ListCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var msg = "**Custom Commands List:**\r\n";
            foreach (var cmd in _config.CustomCommands)
            {
                msg += $"{_config.CommandsPrefix}{cmd.Key}: {cmd.Value}\r\n";
            }
            if (_config.CustomCommands == null || _config.CustomCommands.Count == 0)
            {
                msg += "No custom commands have been created yet.";
            }

            await message.RespondAsync(msg);
        }
    }
}