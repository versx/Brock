namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public class DeleteRolesCommand : ICustomCommand
    {
        private readonly DiscordClient _client;

        public bool AdminCommand => true;

        public DeleteRolesCommand(DiscordClient client)
        {
            _client = client;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            try
            {
                foreach (var role in message.Channel.Guild.Roles)
                {
                    if (Roles.Teams.ContainsKey(role.Name))
                    {
                        await message.Channel.Guild.DeleteRoleAsync(role);
                    }
                }

                await message.RespondAsync("All team roles have been deleted.");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
                await message.RespondAsync("Failed to delete one or more team roles.");
            }
        }
    }
}