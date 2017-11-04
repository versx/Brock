namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Extensions;
    using PokeFilterBot.Utilities;

    public class CreateRolesCommand : ICustomCommand
    {
        private readonly DiscordClient _client;

        public bool AdminCommand => true;

        public CreateRolesCommand(DiscordClient client)
        {
            _client = client;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            foreach (var team in Roles.Teams)
            {
                try
                {
                    if (_client.GetRoleFromName(team.Key) == null)
                    {
                        if (message.Channel.Guild != null)
                        {
                            await message.Channel.Guild.CreateRoleAsync(team.Key, message.Channel.Guild.EveryoneRole.Permissions, team.Value, null, true, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError(ex);
                    await message.RespondAsync($"Failed to create team role {team.Key}, it might already exist or I do not have the correct permissions to manage roles.");
                }
            }

            await message.RespondAsync("Valor, Mystic, and Instinct team roles were successfully created.");
        }
    }
}