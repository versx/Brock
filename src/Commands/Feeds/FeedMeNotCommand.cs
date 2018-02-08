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

            if (!(command.Args.Count == 1))
            {
                await message.RespondAsync($"{message.Author.Mention} please provide correct values such as `{_config.CommandsPrefix}{command.Name} <city_name>`, `{_config.CommandsPrefix}{command.Name} <city1>,<city2>,<city3>`");
                return;
            }

            if (message.Channel.Guild == null)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null) return;

                await message.RespondAsync($"Currently I only support city feed assignment via the channel #{channel.Name}, direct message support is coming soon.");
                return;
            }

            var unassigned = new List<string>();
            var alreadyUnassigned = new List<string>();

            try
            {
                var member = await _client.GetMemberFromUserId(message.Author.Id);
                if (member == null)
                {
                    await message.RespondAsync($"Failed to find member with id {message.Author.Id}.");
                    return;
                }

                var cmd = command.Args[0];
                if (string.Compare(cmd, FeedAll, true) == 0)
                {
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

                    var reason = $"{message.Author.Mention} initiated city assignment removal via {AssemblyUtils.AssemblyName}.";
                    var cityRole = _client.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        await message.RespondAsync($"{message.Author.Mention} {city} is not a valid city feed name.");
                        continue;
                    }

                    if (member.HasRole(cityRole.Id))
                    {
                        //msg += $"{member.Mention} has been removed from city feed {cityRole.Name}. ";
                        await member.RevokeRoleAsync(cityRole, reason);
                        unassigned.Add(cityRole.Name);
                        continue;
                    }

                    alreadyUnassigned.Add(cityRole.Name);
                }

                await message.RespondAsync
                (
                    (unassigned.Count > 0
                        ? $"{message.Author.Mention} has been removed from city feed{(unassigned.Count > 1 ? "s" : null)} **{string.Join("**, **", unassigned)}**."
                        : string.Empty) +
                    (alreadyUnassigned.Count > 0
                        ? $" {message.Author.Mention} is not assigned to **{string.Join("**, **", alreadyUnassigned)}** city feed{(alreadyUnassigned.Count > 1 ? "s" : null)}."
                        : string.Empty)
                );
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