namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.General,
        "Assign yourself to a team role, available teams to join are **Valor**, **Mystic**, and **Instinct**.",
        "\tExample: `.team Valor` (Joins Valor)\r\n" +
        "\tExample: `.iam None` (Leave Team)",
        //CommandPermissionLevel.User,
        "team", "iam"
    )]

    //[
    //    CommandCategory(Categories.General), 
    //    CommandPermission(CommandPermissionLevel.User), 
    //    CommandDescription("Assign yourself to a team role, available teams to join are **Valor**, **Mystic**, and **Instinct**."), 
    //    CommandExample
    //    (
    //        "\tExample: `.team Valor` (Joins Valor)\r\n" +
    //        "\tExample: `.iam None` (Leave Team)"
    //    ),
    //    CommandNames("team", "iam")
    //]
    public class TeamCommand : ICustomCommand
    {
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public TeamCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            Client = client;
            Db = db;
            _config = config;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs)
            {
                await message.RespondAsync($"Team name is required, available teams are {string.Join(", ", _config.TeamRoles)}, and None");
                return;
            }
            if (command.Args.Count != 1) return;

            var team = command.Args[0];
            if (!_config.TeamRoles.Exists(x => string.Compare(team, x, true) == 0 || string.Compare(team, "None", true) == 0))
            {
                await message.RespondAsync($"You have entered an incorrect team name, please enter one of the following: {(string.Join(", ", _config.TeamRoles))}, or None.");
                return;
            }

            try
            {
                await message.IsDirectMessageSupported();

                if (message.Channel.Guild == null)
                {
                    //TODO: Ask what server to assign to.
                    //foreach (var guild in _client.Guilds)
                    //{
                    //    await guild.Value.GrantRoleAsync(member, teamRole, reason);
                    //}
                    var channel = await Client.GetChannel(_config.CommandsChannelId);
                    await message.RespondAsync($"Currently I only support team assignment via the channel #{channel.Name}, direct message support is coming soon.");
                    return;
                }

                var member = await Client.GetMemberFromUserId(message.Author.Id);
                var teamRole = Client.GetRoleFromName(team);
                var reason = $"User initiated team assignment via {AssemblyUtils.AssemblyName}.";
                var alreadyAssigned = false;
                var msg = string.Empty;

                foreach (var role in member.Roles)
                {
                    alreadyAssigned |= role.Name == teamRole.Name;

                    if ((_config.TeamRoles.Exists(x => string.Compare(role.Name, x, true) == 0)) && !alreadyAssigned)
                    {
                        await message.Channel.Guild.RevokeRoleAsync(member, role, reason);
                        msg += $"{message.Author.Mention} has left team {role.Name}. ";
                    }
                }

                if (teamRole != null && !alreadyAssigned)
                {
                    await message.Channel.Guild.GrantRoleAsync(member, teamRole, reason);
                    msg += $"{message.Author.Mention} has joined team {teamRole.Name}.";
                }

                if (alreadyAssigned)
                {
                    msg = $"You are already assigned to team {teamRole.Name}.";
                }

                if (!string.IsNullOrEmpty(msg))
                {
                    await message.RespondAsync(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}