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
        "\tExample: .addcmd google https://google.com\r\n" +
        "\tExample: .addcmd cats http://example.com/cats.gif",
        "addcmd"
    )]
    public class AddCmdCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public AddCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (command.Args.Count != 2) return;

            var cmd = command.Args[0];
            var cmdMsg = command.Args[1];

            if (!_config.CustomCommands.ContainsKey(cmd))
            {
                _config.CustomCommands.Add(cmd, cmdMsg);
                _config.Save();

                await message.RespondAsync($"{message.Author.Mention}, custom command {cmd} has been registered to say {cmdMsg} when triggered.");
            }
            else
            {
                await message.RespondAsync($"{message.Author.Mention}, custom command {cmd} already exists in the custom commands database.");
            }
        }
    }
}