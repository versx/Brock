namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;

    [Command(Categories.Administrative,
        "Switch all level 30 accounts for all city feeds.",
        "\tExample: `.switch-accounts` (Sets a city feed's scan list)",
        "switch-accounts"
    )]
    public class SwitchAccountsCommand : ICustomCommand
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public SwitchAccountsCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;
            //if (!command.HasArgs) return;
            //if (command.Args.Count != 1) return;

            //var cmd = command.Args[0];
            var good = Path.Combine(SetEncounterListCommand.MapPath, "good.txt");
            //if (!File.Exists(good))
            //{
            //    await message.RespondAsync($"{message.Author.Mention}, {cmd}.txt does not exist.");
            //    return;
            //}

            var banned = Path.Combine(SetEncounterListCommand.MapPath, "..\\Accounts - Level 30 (Shadow Banned).txt");

            SwitchLevel30s(_config.CityRoles, SetEncounterListCommand.MapPath, good, banned);
            await message.RespondAsync($"{message.Author.Mention} switched the level 30 accounts successfully.");
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

                var newAccounts = lvl30s.GetRange(0, Math.Max(amount, lvl30s.Count));
                File.WriteAllLines(cityAccountPath, newAccounts);
                lvl30s.RemoveRange(0, Math.Max(amount, lvl30s.Count));
                Console.WriteLine($"Added {newAccounts.Count} accounts to city feed {city}...");
            }

            Console.WriteLine($"Level 30 accounts have been switch for {cities.Count} cities...");

            //TODO: Print remainder accounts unused.
        }
    }
}