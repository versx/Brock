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
        "Lists the current Twitter notification account id's in database.",
        "\tExample: .twitter_list",
        "twitter_list"
    )]
    public class ListTwitterCommand : ICustomCommand
    {
        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public ListTwitterCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var ids = string.Join(", ", Config.TwitterUpdates.TwitterUsers);
            await message.RespondAsync($"You are currently subscribed to the following Twitter account notifications: {ids}");
        }
    }
}