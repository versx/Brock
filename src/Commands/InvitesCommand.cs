namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

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

            await message.IsDirectMessageSupported();

            try
            {
                var invites = await message.Channel.Guild.GetInvitesAsync();
                var msg = string.Empty;
                foreach (var invite in invites)
                {
                    msg +=
                        $"Invite code: **{invite.Code}** created at {invite.CreatedAt.ToString("MM/dd/yyyy hh:mm:ss tt")}\r\n" +
                        $"Invite for {invite.Guild.Name} ({invite.Guild.Id}) channel {invite.Channel.Name} ({invite.Channel.Id})\r\n" +
                        $"Inviter {invite.Inviter.Username} ({invite.Inviter.Id})\r\n" +
                        (invite.Uses == invite.MaxUses ? "Unlimited invite uses." : $"{invite.Uses}/{invite.MaxUses} uses total.") + (invite.IsTemporary ? " Temporary invite" : "");
                }
                await message.RespondAsync(msg);
            }
            catch (Exception ex)
            {
                await message.RespondAsync("It appears that I do not have the correct permissions to perform that command.");
                Utils.LogError(ex);
            }
        }
    }
}