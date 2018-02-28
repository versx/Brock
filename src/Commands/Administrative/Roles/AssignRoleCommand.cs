namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.Administrative,
        "Assigns the mentioned user the mentioned role.",
        "\tExample: `.assign @user @role`",
        "assign"
    )]
    public class AssignRoleCommand : ICustomCommand
    {
        private readonly DiscordClient _client;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public AssignRoleCommand(DiscordClient client)
        {
            _client = client;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            var userMention = command.Args[0];
            var roleMention = command.Args[1];
            var userId = DiscordHelpers.ConvertMentionToUserId(userMention);
            var roleId = DiscordHelpers.ConvertMentionToUserId(roleMention);
            var result = await AssignGuildMemberToRole(message, userId, roleId);
            if (result)
            {
                await message.RespondAsync($"{message.Author.Mention} has added {userMention} to the {roleMention} role.");
                return;
            }

            await message.RespondAsync($"{message.Author.Mention} failed to add {userMention} to the {roleMention} role.");
        }

        private async Task<bool> AssignGuildMemberToRole(DiscordMessage message, ulong userId, ulong roleId)
        {
            var member = await _client.GetMemberFromUserId(userId);
            if (member == null)
            {
                await message.RespondAsync($"Failed to retrieve discord member with id {userId}.");
                return false;
            }

            var result = await _client.AssignRole(member, roleId);
            return result;
        }
    }
}