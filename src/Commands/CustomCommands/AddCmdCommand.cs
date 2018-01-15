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
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public AddCmdCommand(DiscordClient client, IDatabase db, Config config)
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
                Config.CustomCommands.Add(cmd, cmdMsg);
                await message.RespondAsync($"Custom command {cmd} has been registered to say {cmdMsg} when triggered.");
                Config.Save();
            }
            else
            {
                await message.RespondAsync($"Custom command {cmd} already exists in the custom commands database.");
            }
        }
    }
}