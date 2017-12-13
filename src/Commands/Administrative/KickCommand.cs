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
        "Kicks the specified discord user.",
        null,
        "kick"
    )]
    public class KickCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public KickCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count == 1 || command.Args.Count == 2) return;

            await message.IsDirectMessageSupported();

            var userId = command.Args[0];
            var reason = command.Args.Count == 2 ? command.Args[1] : "Unknown Reason";
            var guild = message.Channel.Guild;

            if (!ulong.TryParse(userId, out ulong result))
            {
                await message.RespondAsync($"{userId} is not a valid user id.");
                return;
            }

            var user = await guild.GetMemberAsync(result);
            if (user == null)
            {
                await message.RespondAsync($"Failed to retrieve user with id {userId}.");
                return;
            }

            await message.Channel.Guild.RemoveMemberAsync(user, reason);
            await message.RespondAsync($"User {user.Username} (ID: {user.Id}) was successfully kicked with reason: {reason}");
        }
    }
}