namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.Moderator,
        "Assigns a member the " + TeamEliteRole + " and " + EastLA + " roles.",
        "\tExample: `.elite @mention` (Assigns the mentioned member the " + TeamEliteRole + " and " + EastLA + " roles.)",
        "elite"
    )]
    public class AssignEliteCommand : ICustomCommand
    {
        public const string TeamEliteRole = "TMxEliteEastLA";
        public const string EastLA = "EastLA";

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Moderator;

        public AssignEliteCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.Args.Count != 1) return;

            var mention = command.Args[0];
            var userId = DiscordHelpers.ConvertMentionToUserId(mention);
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

            if (!await _client.AssignRole(member, TeamEliteRole))
            {
                await message.RespondAsync($"{message.Author.Mention} failed to assign {mention} the {TeamEliteRole} role. Please check my permissions and that the role exists.");
            }

            if (!await _client.AssignRole(member, EastLA))
            {
                await message.RespondAsync($"{message.Author.Mention} failed to assign {mention} the {EastLA} role. Please check my permissions and that the role exists.");
            }

            await message.RespondAsync($"{message.Author.Mention} assigned {mention} the {TeamEliteRole} and {EastLA} roles.");
        }
    }
}