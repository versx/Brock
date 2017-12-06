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
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public EditCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            var cmd = command.Args[0];
            var cmdMsg = command.Args[1];

            if (!Config.CustomCommands.ContainsKey(cmd))
            {
                await message.RespondAsync($"Failed to update custom command {cmd} in the database, it is not registered.");
                return;
            }

            Config.CustomCommands[cmd] = cmdMsg;
            await message.RespondAsync($"Custom command {cmd} was successfully updated in the database to say {cmdMsg} when triggered.");
            Config.Save();
        }
    }
}