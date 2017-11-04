namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Configuration;
    using PokeFilterBot.Extensions;
    using PokeFilterBot.Utilities;

    public class IamCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;

        public IamCommand(DiscordClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 1)
            {
                var team = command.Args[0];
                switch (team)
                {
                    case "Valor":
                    case "valor":
                    case "Mystic":
                    case "mystic":
                    case "Instinct":
                    case "instinct":
                    case "None":
                    case "none":
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

                                if ((role.Name == "Valor" || role.Name == "Mystic" || role.Name == "Instinct") && !alreadyAssigned)
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
                        break;
                    default:
                        await message.RespondAsync($"You have entered an incorrect team name, please enter one of the following: Valor, Mystic, or Instinct.");
                        break;
                }
            }
        }
    }
}