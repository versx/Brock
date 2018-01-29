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
        "Unassign yourself from a city feed's role.",
        "\tExample: `.feedmenot Ontario` (Leaves specified city)\r\n" +
        "\tExample: `.feedmenot all` (Leaves all cities)",
        "feedmenot"
    )]
    public class FeedMeNotCommand : ICustomCommand
    {
        public const string FeedAll = "All";

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public FeedMeNotCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            //await message.IsDirectMessageSupported();

            if (message.Channel.Guild == null)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null) return;

                await message.RespondAsync($"Currently I only support city feed assignment via the channel #{channel.Name}, direct message support is coming soon.");
                return;
            }

            try
            {
                var guild = message.Channel.Guild;

                if (command.Args.Count == 1)
                {
                    var msg = string.Empty;
                    var cmd = command.Args[0];

                    if (string.Compare(cmd, FeedAll, true) == 0)
                    {
                        var member = await guild.GetMemberAsync(message.Author.Id);
                        if (member == null)
                        {
                            await message.RespondAsync($"Failed to find member with id {message.Author.Id}.");
                            return;
                        }

                        await RemoveAllDefaultFeedRoles(message, member);
                        return;
                    }

                    var cities = cmd.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var city in cities)
                    {
                        if (!_config.CityRoles.Exists(x => string.Compare(city, x, true) == 0))
                        {
                            await message.RespondAsync($"{message.Author.Mention} has entered an incorrect city feed name, please enter one of the following: {(string.Join(",", _config.CityRoles))}, or {FeedAll}.");
                            continue;
                        }

                        var member = await _client.GetMemberFromUserId(message.Author.Id);
                        var cityRole = _client.GetRoleFromName(city);
                        var reason = $"{message.Author.Mention} initiated city assignment removal via {AssemblyUtils.AssemblyName}.";
                        var alreadyAssigned = false;

                        if (cityRole == null && city != FeedAll)
                        {
                            if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                            msg += $"{city} is not a valid city feed role.";
                            continue;
                        }

                        foreach (var role in member.Roles)
                        {
                            alreadyAssigned |= string.Compare(role.Name, cityRole.Name, true) == 0;
                            if (string.Compare(role.Name, cityRole.Name, true) == 0)
                            {
                                if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                                msg += $"{member.Mention} has been removed from city feed {cityRole.Name}. ";
                                await member.RevokeRoleAsync(cityRole, reason);
                                continue;
                            }
                        }

                        if (!alreadyAssigned)
                        {
                            await message.RespondAsync($"{member.Mention} is not assigned to city feed {cityRole.Name}.");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(msg))
                    {
                        _logger.Error($"FeedMeNot command response message was empty.");
                        return;
                    }

                    await message.RespondAsync(msg);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task RemoveAllDefaultFeedRoles(DiscordMessage message, DiscordMember member)
        {
            var reason = "Default city role assignment removal.";
            foreach (var city in _config.CityRoles)
            {
                var cityRole = _client.GetRoleFromName(city);
                if (cityRole == null)
                {
                    //Failed to find role.
                    _logger.Error($"Failed to find city role {city}, please make sure it exists.");
                    continue;
                }

                await member.RevokeRoleAsync(cityRole, reason);
            }

            await message.RespondAsync($"{member.Mention} was unassigned all city feed roles.");
        }
    }
}