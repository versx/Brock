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
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        public AddTwitterCommand(DiscordClient client, IDatabase db, Config config)
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

            if (Config.TwitterUpdates.TwitterUsers.Contains(result))
            {
                await message.RespondAsync($"{message.Author.Mention} is already subscribed to {result} Twitter notifications.");
                return;
            }

            Config.TwitterUpdates.TwitterUsers.Add(result);
            await message.RespondAsync($"{result} was successfully added to the Twitter notifications database.");
            Config.Save();
        }
    }
}