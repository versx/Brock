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

    [Command(
        Categories.Administrative,
        "Creates the required team roles to be assigned when users type the team assignment commmand.",
        "\tExample: `.create-roles`",
        "create-roles"
    )]
    public class CreateRolesCommand : ICustomCommand
    {
        public const string DefaultParentChannel = "GENERAL";

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public CreateRolesCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            foreach (var team in _config.TeamRoles)
            {
                try
                {
                    if (_client.GetRoleFromName(team) != null)
                    {
                        await message.RespondAsync($"Role {team} already exists.");
                        continue;
                    }

                    var roleColor = Roles.Teams.ContainsKey(team) ? Roles.Teams[team] : DiscordColor.None;
                    var newRole = await message.Channel.Guild.CreateRoleAsync(team, message.Channel.Guild.EveryoneRole.Permissions, roleColor, null, true);
                    if (newRole == null)
                    {
                        _logger.Error($"Failed to create team role {team}");
                        continue;
                    }

                    var parentChannel = _client.GetChannelByName(DefaultParentChannel);
                    if (parentChannel == null)
                    {
                        _logger.Error($"Failed to find parent channel '{DefaultParentChannel}'.");
                        return;
                        //TODO: Create parent channel category if it doesn't exist?
                    }

                    var newChannel = await message.Channel.Guild.CreateChannelAsync($"team_{team.ToLower()}", ChannelType.Text, parentChannel);
                    if (newChannel == null)
                    {
                        _logger.Error($"Failed to create team channel team_{team.ToLower()}");
                        return;
                    }

                    await newChannel.GrantPermissions(message.Channel.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages | Permissions.ReadMessageHistory);
                    await newChannel.GrantPermissions(newRole, Permissions.SendMessages | Permissions.ReadMessageHistory, Permissions.None);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    await message.RespondAsync($"{message.Author.Mention}, failed to create team role {team}, it might already exist or I do not have the correct permissions to manage roles.");
                }
            }

            await message.RespondAsync($"{message.Author.Mention}, {string.Join(", ", _config.TeamRoles)} team roles were successfully created.");

            foreach (var city in _config.CityRoles)
            {
                try
                {
                    if (_client.GetRoleFromName(city) != null)
                    {
                        await message.RespondAsync($"{message.Author.Mention}, role {city} already exists.");
                        return;
                    }

                    var role = await message.Channel.Guild.CreateRoleAsync(city, message.Channel.Guild.EveryoneRole.Permissions, null, null, true);
                    if (role == null)
                    {
                        _logger.Error($"Failed to create team role {city}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    await message.RespondAsync($"{message.Author.Mention}, failed to create team role {city}, it might already exist or I do not have the correct permissions to manage roles.");
                }
            }

            await message.RespondAsync($"{message.Author.Mention}, {string.Join(", ", _config.CityRoles)} city roles were successfully created.");
        }
    }
}