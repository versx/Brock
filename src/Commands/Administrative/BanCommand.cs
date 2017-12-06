namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Administrative,
        "Bans the specified user id from the current guild.",
        "\tExample: .ban 324234234324 \"Spamming channels.\"\r\n" +
        "\tExample: .ban 233435253232",
        "ban"
    )]
    public class BanCommand : ICustomCommand
    {
        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public BanCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count == 1 || command.Args.Count == 2) return;

            var userId = command.Args[0];
            var reason = command.Args.Count == 2 ? command.Args[1] : "Unknown Reason";

            if (message.Channel == null)
            {
                await message.RespondAsync("DM is not supported for this command yet.");
                return;
            }

            if (!ulong.TryParse(userId, out ulong result))
            {
                await message.RespondAsync($"{userId} is not a valid user id.");
                return;
            }

            var user = await Client.GetMemberFromUserId(result);

            await message.Channel.Guild.BanMemberAsync(result, 365, reason);
            await message.RespondAsync($"User {user.Username} (ID: {user.Id}) was successfully banned with reason: {reason}");
        }
    }
}