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
        "Deletes the specified Twitter account's id from the Twitter notifications database.",
        "\tExample: .twitter_del 302984039248",
        "twitter_del"
    )]
    public class DeleteTwitterCommand : ICustomCommand
    {
        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public DeleteTwitterCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            var twitterId = command.Args[0];
            if (!ulong.TryParse(twitterId, out ulong result))
            {
                await message.RespondAsync($"{twitterId} is not a valid Twitter id.");
                return;
            }

            if (!Config.TwitterUpdates.TwitterUsers.Contains(result))
            {
                await message.RespondAsync($"You are not current subscribed to {result} Twitter notifications.");
                return;
            }

            if (!Config.TwitterUpdates.TwitterUsers.Remove(result))
            {
                await message.RespondAsync($"Failed to remove {result} Twitter notifications.");
                return;
            }

            await message.RespondAsync($"{result} was successfully removed from the Twitter notifications database.");
            Config.Save();
        }
    }
}