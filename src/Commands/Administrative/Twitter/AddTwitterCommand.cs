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
        "Adds the specified Twitter account's id to the Twitter notifications database.",
        "\tExample: .twitter_add 302984039248",
        "twitter_add"
    )]
    public class AddTwitterCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public AddTwitterCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            var twitterId = command.Args[0];
            if (!ulong.TryParse(twitterId, out ulong result))
            {
                await message.RespondAsync($"{message.Author.Mention}, {twitterId} is not a valid Twitter id.");
                return;
            }

            if (_config.TwitterUpdates.TwitterUsers.Contains(result))
            {
                await message.RespondAsync($"{message.Author.Mention} is already subscribed to {result} Twitter notifications.");
                return;
            }

            _config.TwitterUpdates.TwitterUsers.Add(result);
            _config.Save();

            await message.RespondAsync($"{message.Author.Mention}, {result} was successfully added to the Twitter notifications database.");
        }
    }
}