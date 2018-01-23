namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Services;

    using DSharpPlus;
    using DSharpPlus.Entities;

    [Command(Categories.General,
        "Sets the encounter list for a specific city feed.",
        "\tExample: `.setlist Upland Clear` (Sets a city feed's scan list)\r\n" +
        "\tExample: `.setlist all Clear` (Sets all city feed's scan list)",
        "setlist"
    )]
    public class SetEncounterListCommand : ICustomCommand
    {
        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public IEventLogger Logger { get; }

        #endregion

        #region Constructor

        public SetEncounterListCommand(DiscordClient client, IDatabase db, IEventLogger logger)
        {
            Client = client;
            Db = db;
            Logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs || command.Args.Count != 2) return;

            const string BasePath = @"C:\Users\Jeremy\Sync\RocketMap";
            var city = command.Args[0];
            var weather = command.Args[1];
            if (weather.TryParse(out WeatherType result))
            {
                if (!SwitchEncounterList(BasePath, $"RM-{city}", result))
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to switch encounter list for {city} to {result}.");
                    return;
                }

                await message.RespondAsync($"{message.Author.Mention} switched encounter list for {city} to {result}.");
            }
        }

        private void SwitchLevel30s(List<string> cities, string mapPath, string lvl30sFilePath, string bannedLvl30sFilePath)
        {
            var lvl30s = new List<string>(File.ReadAllLines(lvl30sFilePath));
            var amount = lvl30s.Count / cities.Count;

            foreach (var city in cities)
            {
                var cityAccountPath = Path.Combine(mapPath, $"high-level_{city}.csv");
                var oldAccounts = File.ReadAllLines(cityAccountPath);
                File.AppendAllLines(bannedLvl30sFilePath, oldAccounts);
                Console.WriteLine($"Removed {oldAccounts.Length} accounts from city feed {city}...");

                var newAccounts = lvl30s.GetRange(0, amount);
                File.WriteAllLines(cityAccountPath, newAccounts);
                lvl30s.RemoveRange(0, amount);
                Console.WriteLine($"Added {newAccounts.Count} accounts to city feed {city}...");
            }

            Console.WriteLine($"Level 30 accounts have been switch for {cities.Count} cities...");

            //TODO: Print remainder accounts unused.
        }

        private bool SwitchEncounterList(string mapPath, string feedName, WeatherType weather)
        {
            Logger.Trace($"SetEncounterListCommand::SwitchEncounterList [MapPath={mapPath}, FeedName={feedName}, WeatherType={weather}]");

            Logger.Debug($"Attempting to switch encounter list for feed {feedName} to {weather}...");

            var encounterList = "enc-whitelist-rares";
            var baseEncounterListFilePath = Path.Combine(mapPath, $"{encounterList}.txt");
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

            Logger.Debug($"Stopping feed {feedName}...");
            if (!TaskManager.StopTask(feedName))
            {
                Logger.Error($"Failed to stop feed {feedName}, failed to switch encounter list...");
                return false;
            }

            Logger.Debug("Writing new weather encounter list...");
            File.WriteAllLines(baseEncounterListFilePath, weatherEncounterList);

            Logger.Debug($"Starting feed {feedName}...");
            if (!TaskManager.StartTask(feedName))
            {
                Logger.Error($"Failed to start feed {feedName}, failed to switch encounter list...");
                return false;
            }

            Logger.Debug($"Successfully switched encounter list for feed {feedName} to {weather}...");
            return true;
        }
    }
}