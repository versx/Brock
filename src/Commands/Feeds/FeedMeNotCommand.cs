namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    //TODO: Parse feed cities with spaces, or replace all feeds with a single command.

    [Command(Categories.General,
        "Unassign yourself from a city feed's role.",
        "\tExample: `.feedmenot Ontario` (Leaves specified city)\r\n" +
        "\tExample: `.feedmenot all` (Leaves all cities)",
        "feedmenot"//, "city"
    )]
    public class FeedMeNotCommand : ICustomCommand
    {
        public const string FeedAll = "All";

        private readonly Config _config;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public FeedMeNotCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (message.Channel.Guild == null)
            {
                //TODO: Ask what server to assign to.
                //foreach (var guild in _client.Guilds)
                //{
                //    await guild.Value.GrantRoleAsync(member, teamRole, reason);
                //}
                var channel = await Client.GetChannel(_config.CommandsChannelId);
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
                            await message.RespondAsync($"{message.Author.Username} has entered an incorrect city name, please enter one of the following: {(string.Join(",", _config.CityRoles))}, or {FeedAll}.");
                            continue;
                        }

                        var member = await Client.GetMemberFromUserId(message.Author.Id);
                        var cityRole = Client.GetRoleFromName(city);
                        var reason = $"User initiated city assignment removal via {AssemblyUtils.AssemblyName}.";
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
                                msg += $"{member.Username} has been removed from city feed {cityRole.Name}. ";
                                await member.RevokeRoleAsync(cityRole, reason);
                                continue;
                            }
                        }

                        if (!alreadyAssigned)
                        {
                            await message.RespondAsync($"{member.Username} is not assigned to city feed {cityRole.Name}.");
                            continue;
                        }
                    }

                    await message.RespondAsync(msg);
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }

        private async Task RemoveAllDefaultFeedRoles(DiscordMessage message, DiscordMember member)
        {
            var reason = "Default city role assignment removal.";
            foreach (var city in _config.CityRoles)
            {
                var cityRole = Client.GetRoleFromName(city);
                if (cityRole == null)
                {
                    //Failed to find role.
                    Utils.LogError(new Exception($"Failed to find city role {city}, please make sure it exists."));
                    continue;
                }

                await member.RevokeRoleAsync(cityRole, reason);
            }

            await message.RespondAsync($"{member.Username} was unassigned all default city feed roles.");
        }
    }
}