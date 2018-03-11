namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    using DSharpPlus;
    using DSharpPlus.Entities;

    [Command(Categories.Administrative,
        "Sets the encounter list for a specific city feed.",
        "\tExample: `.setlist Upland Clear` (Sets a city feed's scan list)\r\n" +
        "\tExample: `.setlist Upland,Pomona,EastLA PartlyCloudy` (Sets multiple city feed scan lists)\r\n" +
        "\tExample: `.setlist all windy` (Sets all city feed's scan list)",
        "setlist"
    )]
    public class SetEncounterListCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IEventLogger _logger;
        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Moderator;

        #endregion

        #region Constructor

        public SetEncounterListCommand(DiscordClient client, IEventLogger logger, Config config)
        {
            _client = client;
            _logger = logger;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs || command.Args.Count != 2) return;

            var city = command.Args[0];
            var weather = command.Args[1];
            if (weather.TryParse(out WeatherType result))
            {
                if (string.Compare(city, "all", true) == 0)
                {
                    var switched = new List<string>();
                    foreach (var feed in _config.CityRoles)
                    {
                        if (string.IsNullOrEmpty(feed)) continue;

                        //TODO: Find better way to skip these three without hard coding it.
                        if (feed == "Raids" || feed == "Nests" || feed == "Families") continue;

                        if (!SetEncounterList(_config.MapFolder, feed, result))
                        {
                            await message.RespondAsync($"{message.Author.Mention}, failed to switch encounter list for {feed} to {result}.");
                            continue;
                        }

                        switched.Add(feed);
                    }

                    await message.RespondAsync($"{message.Author.Mention} switched encounter list for {string.Join(", ", switched)} to {result}.");
                    return;
                }

                if (city == null)
                {
                    await message.RespondAsync($"{message.Author.Mention} you've specified an invalid city '{city}'.");
                    return;
                }

                if (!SetEncounterList(_config.MapFolder, city, result))
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to switch encounter list for {city} to {result}.");
                    return;
                }

                await message.RespondAsync($"{message.Author.Mention} switched encounter list for {city} to {result}.");
                return;
            }

            await message.RespondAsync($"{message.Author.Mention} specified an invalid Weather type.");
        }

        private bool SetEncounterList(string mapPath, string feedName, WeatherType weather)
        {
            _logger.Trace($"SetEncounterListCommand::SwitchEncounterList [MapPath={mapPath}, FeedName={feedName}, WeatherType={weather}]");

            _logger.Debug($"Attempting to switch encounter list for feed {feedName} to {weather}...");

            var encounterList = "enc-whitelist-rares";
            var cityEncounterListFilePath = Path.Combine(mapPath, $"{encounterList}-{feedName}.txt");
            if (!File.Exists(cityEncounterListFilePath))
            {
                _logger.Error($"Specified city encounter list does not exist at path {cityEncounterListFilePath}...");
                return false;
            }

            var weatherEncounterListFilePath = Path.Combine(mapPath, $"{encounterList} - {weather}.txt");
            if (!File.Exists(weatherEncounterListFilePath))
            {
                _logger.Error($"Specified weather encounter list does not exist at path {weatherEncounterListFilePath}...");
                return false;
            }

            var weatherEncounterList = File.ReadAllLines(weatherEncounterListFilePath);
            if (weatherEncounterList.Length == 0)
            {
                _logger.Error($"Encounter list is empty, aborting...");
                return false;
            }

            _logger.Debug("Writing new weather encounter list...");
            File.WriteAllLines(cityEncounterListFilePath, weatherEncounterList);

            _logger.Debug($"Successfully switched encounter list for feed {feedName} to {weather}...");
            return true;
        }
    }
}