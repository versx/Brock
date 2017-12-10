﻿namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.General,
        "Displays a list of available assignable team roles.",
        "\tExample: `.teams`",
        "teams"
    )]
    public class TeamsCommand : ICustomCommand
    {
        private readonly Config _config;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public TeamsCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                $"**Available Assignable Teams:**\r\n{string.Join(Environment.NewLine, _config.TeamRoles)}"
            );
        }
    }

    [Command(Categories.General,
        "Displays a list of available assignable city feed roles.",
        "\tExample: `.feeds`",
        "feeds"
    )]
    public class CitiesCommand : ICustomCommand
    {
        private readonly Config _config;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public CitiesCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                $"**Available Assignable Cities:**\r\n{string.Join(Environment.NewLine, _config.CityRoles)}"
            );
        }
    }

    [Command(Categories.General,
        "Assign yourself to a team role, available teams to join are **Valor**, **Mystic**, and **Instinct**.",
        "\tExample: `.team Valor` (Joins Valor)\r\n" +
        "\tExample: `.iam None` (Leave Team)",
        "team", "iam"
    )]
    public class TeamCommand : ICustomCommand
    {
        private readonly Config _config;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public TeamCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
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
                //TODO: Only retrieve the current guild.
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
                        msg += $"{message.Author.Username} has left team {role.Name}. ";
                    }
                }

                if (teamRole != null && !alreadyAssigned)
                {
                    await message.Channel.Guild.GrantRoleAsync(member, teamRole, reason);
                    msg += $"{message.Author.Username} has joined team {teamRole.Name}.";
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
                Utils.LogError(ex);
            }
        }
    }
}