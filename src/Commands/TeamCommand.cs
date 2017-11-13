namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    public class TeamCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;

        public bool AdminCommand => false;

        public TeamCommand(DiscordClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var team = command.Args[0];
            if (_config.AvailableTeamRoles.Exists(x => string.Compare(team, x, true) == 0 || string.Compare(team, "None", true) == 0))
            {
                try
                {
                    var member = await _client.GetMemberFromUserId(message.Author.Id);
                    var teamRole = _client.GetRoleFromName(team);
                    var reason = $"User initiated team assignment via {AssemblyUtils.AssemblyName}.";
                    //TODO: Only retrieve the current guild.
                    if (message.Channel.Guild == null)
                    {
                        //TODO: Ask what server to assign to.
                        //foreach (var guild in _client.Guilds)
                        //{
                        //    await guild.Value.GrantRoleAsync(member, teamRole, reason);
                        //}
                        await message.RespondAsync($"Currently I only support team assignment via the channel #{_config.CommandsChannel}, direct message support is coming soon.");
                        return;
                    }

                    bool alreadyAssigned = false;
                    foreach (var role in member.Roles)
                    {
                        alreadyAssigned |= role.Name == teamRole.Name;

                        if ((_config.AvailableTeamRoles.Exists(x => string.Compare(role.Name, x, true) == 0)) && !alreadyAssigned)
                        {
                            await message.Channel.Guild.RevokeRoleAsync(member, role, reason);
                            await message.RespondAsync($"{message.Author.Username} has left team {role.Name}.");
                        }
                    }

                    if (teamRole != null && !alreadyAssigned)
                    {
                        await message.Channel.Guild.GrantRoleAsync(member, teamRole, reason);
                        await message.RespondAsync($"{message.Author.Username} has joined team {teamRole.Name}.");
                    }

                    if (alreadyAssigned)
                    {
                        await message.RespondAsync($"You are already assigned to team {teamRole.Name}.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex}");
                }
            }
            else
            {
                await message.RespondAsync($"You have entered an incorrect team name, please enter one of the following: {(string.Join(", ", _config.AvailableTeamRoles))}, or None.");
            }
        }
    }
}