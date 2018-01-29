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
        "Adds the specified custom command to the database.",
        "\tExample: .delcmd google\r\n" +
        "\tExample: .delcmd cats",
        "delcmd"
    )]
    public class DelCmdCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public DelCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (command.Args.Count != 1) return;

            var cmd = command.Args[0];
            if (_config.CustomCommands.ContainsKey(cmd))
            {
                if (!_config.CustomCommands.Remove(cmd))
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to delete custom command {cmd} from the database, unknown error.");
                    return;
                }

                _config.Save();

                await message.RespondAsync($"{message.Author.Mention}, custom command {cmd} was successfully deleted from the database.");
            }
            else
            {
                await message.RespondAsync($"{message.Author.Mention}, failed to delete custom command {cmd} from the database, it is not registered.");
            }
        }
    }
}