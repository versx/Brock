namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.Moderator,
        "Assigns a member a role.",
        "\tExample: `.elite @mention` (Assigns the mentioned member the " + AssignEliteCommand.TeamEliteRole + " role.)",
        "elite"
    )]
    public class AssignEliteCommand : ICustomCommand
    {
        public const string TeamEliteRole = "TMxEliteEastLA";

        private readonly DiscordClient _client;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Moderator;

        public AssignEliteCommand(DiscordClient client, IEventLogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.Args.Count != 1) return;

            var mention = command.Args[0];
            var userId = ConvertMentionToUserId(mention);
            if (userId == 0)
            {
                await message.RespondAsync($"{message.Author.Mention}, failed to find user {mention}.");
                return;
            }

            var member = await _client.GetMemberFromUserId(userId);
            if (member == null)
            {
                _logger.Error($"Failed to find member with user id {userId}.");
                return;
            }

            var role = _client.GetRoleFromName(TeamEliteRole);
            if (role == null)
            {
                _logger.Error($"Failed to find role '{TeamEliteRole}'.");
                return;
            }

            if (member.HasRole(role.Id))
            {
                await message.RespondAsync($"{message.Author.Mention}, {member.Username} is already assigned {TeamEliteRole} role.");
                return;
            }

            try
            {
                await member.GrantRoleAsync(role);
                await message.RespondAsync($"{message.Author.Mention} has assigned {mention} the {TeamEliteRole} role.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                await message.RespondAsync($"{message.Author.Mention}, failed to assign role {TeamEliteRole} to {mention}, please check my permissions.");
            }
        }

        private ulong ConvertMentionToUserId(string mention)
        {
            //<@201909896357216256>
            mention = Utils.GetBetween(mention, "<", ">");
            mention = mention.Replace("@", null);
            mention = mention.Replace("!", null);

            if (ulong.TryParse(mention, out ulong result))
            {
                return result;
            }

            return 0;
        }
    }
}