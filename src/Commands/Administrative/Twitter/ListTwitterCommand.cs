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
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public ListTwitterCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var ids = string.Join(", ", _config.TwitterUpdates.TwitterUsers);
            await message.RespondAsync($"You are currently subscribed to the following Twitter account notifications: {ids}");
        }
    }
}