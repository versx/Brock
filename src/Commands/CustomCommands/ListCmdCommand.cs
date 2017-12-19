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
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public ListCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var msg = "**Custom Commands List:**\r\n";
            foreach (var cmd in Config.CustomCommands)
            {
                msg += $"{Config.CommandsPrefix}{cmd.Key}: {cmd.Value}\r\n";
            }
            if (Config.CustomCommands == null || Config.CustomCommands.Count == 0)
            {
                msg += "No custom commands have been created yet.";
            }

            await message.RespondAsync(msg);
        }
    }
}