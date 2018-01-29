namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    [Command(Categories.Administrative,
        "Assigns the default city roles to all guild members.",
        "\tExample: `.assign_all` (Assigns all guild members the default city roles.)\r\n" +
        "\tExample: `.assign_all NewRoleName (Assigns all guild members the specified role.)`",
        "assign-all"
    )]
    public class AssignRolesCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public AssignRolesCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
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

            if (!command.HasArgs)
            {
                if (_config.CityRoles.Count == 0)
                {
                    await message.RespondAsync("There are currently no city feed roles to assign.");
                    return;
                }

                AssignGuildMembersToRole(message, _config.CityRoles, true);
                return;
            }

            if (command.Args.Count == 1)
            {
                var role = command.Args[0];
                AssignGuildMembersToRole(message, new List<string> { role }, false);
            }
        }

        private void AssignGuildMembersToRole(DiscordMessage message, List<string> roles, bool defaultCityFeed)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x =>
#pragma warning restore RECS0165
            {
                var guild = message.Channel.Guild;
                var success = 0;
                var errors = 0;
                var failed = new List<string>();

                await message.RespondAsync
                (
                    defaultCityFeed
                    ? $"Starting default city feed assignment for all users of guild **{guild.Name}**."
                    : $"Starting {string.Join(",", roles)} role(s) assignment to all users of guild **{guild.Name}**."
                );

                foreach (var member in guild.Members)
                {
                    try
                    {
                        foreach (var role in roles)
                        {
                            var discordRole = _client.GetRoleFromName(role);
                            if (discordRole == null)
                            {
                                //Failed to find role.
                                _logger.Error($"Failed to find city role {role}, please make sure it exists.");
                                continue;
                            }

                            await member.GrantRoleAsync(discordRole, $"{discordRole.Name} role assignment.");
                            _logger.Info($"Assigned {member.Username} to role {discordRole.Name}.");
                        }
                        success++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        failed.Add(member.Username);
                        _logger.Error(ex);
                    }
                }

                _logger.Info($"Finished assigning {string.Join(",", roles)} roles.");

                await message.RespondAsync
                (
                    $"{success}/{guild.MemberCount} members were assigned the " + (defaultCityFeed ? "default city feed" : string.Join(",", roles)) + $" roles and {errors} member's roles were not set." +
                    (failed.Count == 0 ? "" : "\r\nList of users role assignment failed:\r\n" + string.Join(Environment.NewLine, failed))
                );
            })
            { IsBackground = true }.Start();
        }
    }
}