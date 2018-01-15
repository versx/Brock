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
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Supporter;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public DelCmdCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (command.Args.Count != 1) return;

            var cmd = command.Args[0];
            if (Config.CustomCommands.ContainsKey(cmd))
            {
                if (!Config.CustomCommands.Remove(cmd))
                {
                    await message.RespondAsync($"Failed to delete custom command {cmd} from the database, unknown error.");
                    return;
                }

                await message.RespondAsync($"Custom command {cmd} was successfully deleted from the database.");
                Config.Save();
            }
            else
            {
                await message.RespondAsync($"Failed to delete custom command {cmd} from the database, it is not registered.");
            }
        }
    }
}