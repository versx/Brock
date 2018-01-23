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

    //TODO: Parse feed cities with spaces, or replace all feeds with a single command.

    [Command(Categories.General,
        "Assign yourself to a city feed's role.",
        "\tExample: `.feedme Upland,ontario,newport` (Joins a city)\r\n" +
        "\tExample: `.feedme all` (Joins all cities)",
        "feedme"
    )]
    public class FeedMeCommand : ICustomCommand
    {
        public const string FeedAll = "All";

        private readonly Config _config;
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public FeedMeCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            Client = client;
            Db = db;
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
                var channel = await Client.GetChannel(_config.CommandsChannelId);
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

                        await AssignAllDefaultFeedRoles(message, member);
                        return;
                    }

                    var cities = cmd.Replace(" ", "").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var city in cities)
                    {
                        if (!_config.CityRoles.Exists(x => string.Compare(city, x, true) == 0))
                        {
                            await message.RespondAsync($"{message.Author.Mention} has entered an incorrect city name, please enter one of the following: {(string.Join(",", _config.CityRoles))}, or {FeedAll}.");
                            continue;
                        }

                        var member = await Client.GetMemberFromUserId(message.Author.Id);
                        var cityRole = Client.GetRoleFromName(city);
                        var reason = $"User initiated city assignment via {AssemblyUtils.AssemblyName}.";
                        var alreadyAssigned = false;

                        if (cityRole == null)
                        {
                            if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                            msg += $"{city} is not a valid city feed.";
                            continue;
                        }

                        foreach (var role in member.Roles)
                        {
                            alreadyAssigned |= role.Name == cityRole.Name;
                        }

                        if (alreadyAssigned)
                        {
                            if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                            msg += $"{message.Author.Mention} is already assigned to city feed {cityRole.Name}. ";
                            continue;
                        }

                        await message.Channel.Guild.GrantRoleAsync(member, cityRole, reason);

                        if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                        msg += $"{message.Author.Mention} has joined city feed {cityRole.Name}. ";
                    }

                    await message.RespondAsync(msg);
                }
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
                var cityRole = Client.GetRoleFromName(city);
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