namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command(
        Categories.Twitter,
        "Enables or disables the Twitter account nofications in database.",
        "\tExample: .twitter true\r\n" +
        "\tExample: .twitter false",
        "twitter"
    )]
    public class EnableDisableTwitterCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public EnableDisableTwitterCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var enable = command.Args[0];
            if (!bool.TryParse(enable, out bool result))
            {
                await message.RespondAsync($"{enable} is not a valid value, please enter either true or false.");
                return;
            }

            Config.TwitterUpdates.PostTwitterUpdates = result;
            await message.RespondAsync($"All Twitter notifications have been {(result ? "en" : "dis")}abled.");
        }
    }
}