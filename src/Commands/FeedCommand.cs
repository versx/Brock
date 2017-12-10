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

    [Command(Categories.General,
        "Assign yourself to a city role.",
        "\tExample: `.feed Upland` (Joins city)\r\n" +
        "\tExample: `.feed remove Ontario` (Leaves specified city)\r\n" +
        "\tExample: `.feed None` (Leaves all cities)",
        "feed"
    )]
    public class FeedCommand : ICustomCommand
    {
        public const string TeamNone = "None";

        private readonly Config _config;

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public FeedCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            //TODO: Only retrieve the current guild.
            if (message.Channel.Guild == null)
            {
                //TODO: Ask what server to assign to.
                //foreach (var guild in _client.Guilds)
                //{
                //    await guild.Value.GrantRoleAsync(member, teamRole, reason);
                //}
                var channel = await Client.GetChannel(_config.CommandsChannelId);
                await message.RespondAsync($"Currently I only support city assignment via the channel #{channel.Name}, direct message support is coming soon.");
                return;
            }

            try
            {
                if (command.Args.Count == 1)
                {
                    var msg = string.Empty;
                    var cities = command.Args[0].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var city in cities)
                    {
                        if (!_config.CityRoles.Exists(x => string.Compare(city, x, true) == 0 || string.Compare(city, TeamNone, true) == 0))
                        {
                            await message.RespondAsync($"You have entered an incorrect city name, please enter one of the following: {(string.Join(", ", _config.CityRoles))}, or {TeamNone}.");
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
                            msg += $"You are already assigned to city feed {cityRole.Name}. ";
                            continue;
                        }

                        await message.Channel.Guild.GrantRoleAsync(member, cityRole, reason);

                        if (!string.IsNullOrEmpty(msg)) msg += "\r\n";
                        msg += $"{message.Author.Username} has joined city feed {cityRole.Name}. ";
                    }

                    await message.RespondAsync(msg);
                }
                else if (command.Args.Count == 2)
                {
                    var msg = string.Empty;
                    var remove = command.Args[0];
                    var cities = command.Args[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var city in cities)
                    {
                        if (!_config.CityRoles.Exists(x => string.Compare(city, x, true) == 0 || string.Compare(city, TeamNone, true) == 0))
                        {
                            await message.RespondAsync($"{message.Author.Username} has entered an incorrect city name, please enter one of the following: {(string.Join(", ", _config.CityRoles))}, or {TeamNone}.");
                            continue;
                        }

                        var member = await Client.GetMemberFromUserId(message.Author.Id);
                        var cityRole = Client.GetRoleFromName(city);
                        var reason = $"User initiated city assignment removal via {AssemblyUtils.AssemblyName}.";
                        var alreadyAssigned = false;

                        if (cityRole == null)
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
    }
}