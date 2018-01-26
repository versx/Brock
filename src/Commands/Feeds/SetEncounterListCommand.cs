namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    using DSharpPlus;
    using DSharpPlus.Entities;

    [Command(Categories.Administrative,
        "Sets the encounter list for a specific city feed.",
        "\tExample: `.setlist Upland Clear` (Sets a city feed's scan list)\r\n" +
        "\tExample: `.setlist all Clear` (Sets all city feed's scan list)",
        "setlist"
    )]
    public class SetEncounterListCommand : ICustomCommand
    {
        public const string MapPath = @"C:\Users\Jeremy\Sync\PoGO\RocketMap";

        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public IEventLogger Logger { get; }

        #endregion

        #region Constructor

        public SetEncounterListCommand(DiscordClient client, IDatabase db, IEventLogger logger, Config config)
        {
            Client = client;
            Db = db;
            Logger = logger;
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
                        if (!SetEncounterList(MapPath, feed, result))
                        {
                            await message.RespondAsync($"{message.Author.Mention}, failed to switch encounter list for {feed} to {result}.");
                            continue;
                        }

                        switched.Add(feed);
                    }

                    await message.RespondAsync($"{message.Author.Mention} switched encounter list for {string.Join(", ", switched)} to {result}.");
                    return;
                }

                if (!SetEncounterList(MapPath, city, result))
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
            Logger.Trace($"SetEncounterListCommand::SwitchEncounterList [MapPath={mapPath}, FeedName={feedName}, WeatherType={weather}]");

            Logger.Debug($"Attempting to switch encounter list for feed {feedName} to {weather}...");

            var encounterList = "enc-whitelist-rares";
            var cityEncounterListFilePath = Path.Combine(mapPath, $"{encounterList}-{feedName}.txt");
            if (!File.Exists(cityEncounterListFilePath))
            {
                Logger.Error($"Specified city encounter list does not exist at path {cityEncounterListFilePath}...");
                return false;
            }

            var weatherEncounterListFilePath = Path.Combine(mapPath, $"{encounterList} - {weather}.txt");
            if (!File.Exists(weatherEncounterListFilePath))
            {
                Logger.Error($"Specified weather encounter list does not exist at path {weatherEncounterListFilePath}...");
                return false;
            }

            var weatherEncounterList = File.ReadAllLines(weatherEncounterListFilePath);
            if (weatherEncounterList.Length == 0)
            {
                Logger.Error($"Encounter list is empty, aborting...");
                return false;
            }

            Logger.Debug("Writing new weather encounter list...");
            File.WriteAllLines(cityEncounterListFilePath, weatherEncounterList);

            Logger.Debug($"Successfully switched encounter list for feed {feedName} to {weather}...");
            return true;
        }
    }
}