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
        "Edits a specific custom command in the database to say something new.",
        "\tExample: .editcmd google https://google.com/maps\r\n" +
        "\tExample: .editcmd cats http://example.com/lolcat.gif",
        "editcmd"
    )]
    public class EditCmdCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public EditCmdCommand(DiscordClient client, IDatabase db, Config config)
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
                await message.RespondAsync($"{message.Author.Mention}, failed to update custom command {cmd} in the database, it is not registered.");
                return;
            }

            _config.CustomCommands[cmd] = cmdMsg;
            _config.Save();

            await message.RespondAsync($"{message.Author.Mention}, custom command {cmd} was successfully updated in the database to say {cmdMsg} when triggered.");
        }
    }
}