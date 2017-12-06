namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.General,
        "Displays all current invites for the current guild.",
        null,
        "invites"
    )]
    public class InvitesCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            if (message.Channel == null)
            {
                await message.RespondAsync("DM is not supported yet for this command.");
                return;
            }

            var invites = await message.Channel.Guild.GetInvitesAsync();
            var msg = string.Empty;
            foreach (var invite in invites)
            {
                msg +=
                    $"Created at {invite.CreatedAt}\r\nInvite code: {invite.Code}" +
                    $"Invite for {invite.Guild.Name} ({invite.Guild.Id})" +
                    $"Inviter {invite.Inviter.Username} ({invite.Inviter.Id})" +
                    $"{invite.Uses}/{invite.MaxUses} uses total. (Temporary Invite: {(invite.IsTemporary ? "Yes" : "No")}";
            }
            await message.RespondAsync(msg);
        }
    }
}