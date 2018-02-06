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
        "Assign yourself to a city feed's role.",
        "\tExample: `.feedme Upland,ontario,newport` (Joins a city)\r\n" +
        "\tExample: `.feedme all` (Joins all cities)",
        "feedme"
    )]
    public class FeedMeCommand : ICustomCommand
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

        public FeedMeCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
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

            try
            {
                var member = await _client.GetMemberFromUserId(message.Author.Id);
                if (member == null)
                {
                    await message.RespondAsync($"Failed to find member with id {message.Author.Id}.");
                    return;
                }

                var guild = message.Channel.Guild;
                var cmd = command.Args[0];
                if (string.Compare(cmd, FeedAll, true) == 0)
                {
                    await AssignAllDefaultFeedRoles(message, member);
                    return;
                }

                var assigned = new List<string>();
                var alreadyAssigned = new List<string>();

                var cities = cmd.Replace(" ", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var city in cities)
                {
                    if (!_config.CityRoles.Exists(x => string.Compare(city, x, true) == 0))
                    {
                        await message.RespondAsync($"{message.Author.Mention} has entered an incorrect city name, please enter one of the following: {(string.Join(",", _config.CityRoles))}, or {FeedAll}.");
                        continue;
                    }

                    var reason = $"User initiated city assignment via {AssemblyUtils.AssemblyName}.";
                    var cityRole = _client.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        await message.RespondAsync($"{message.Author.Mention} {city} is not a valid city feed name.");
                        continue;
                    }

                    if (member.HasRole(cityRole.Id))
                    {
                        alreadyAssigned.Add(cityRole.Name);
                        continue;
                    }

                    await message.Channel.Guild.GrantRoleAsync(member, cityRole, reason);
                    assigned.Add(cityRole.Name);
                }

                await message.RespondAsync
                (
                    (assigned.Count > 0
                        ? $"{message.Author.Mention} has joined city feed{(assigned.Count > 1 ? "s" : null)} **{string.Join("**, **", assigned)}**."
                        : string.Empty) +
                    (alreadyAssigned.Count > 0
                        ? $" {message.Author.Mention} is already assigned to **{string.Join("**, **", alreadyAssigned)}** city feed{(alreadyAssigned.Count > 1 ? "s" : null)}."
                        : string.Empty)
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task AssignAllDefaultFeedRoles(DiscordMessage message, DiscordMember member)
        {
            var reason = "Default city role assignment initialization.";
            foreach (var city in _config.CityRoles)
            {
                var cityRole = _client.GetRoleFromName(city);
                if (cityRole == null)
                {
                    //Failed to find role.
                    _logger.Error($"Failed to find city role {city}, please make sure it exists.");
                    continue;
                }

                await member.GrantRoleAsync(cityRole, reason);
            }

            await message.RespondAsync($"{member.Mention} was assigned all city feed roles.");
        }
    }
}