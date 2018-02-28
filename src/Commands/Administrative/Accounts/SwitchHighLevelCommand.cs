namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;

    [Command(
        Categories.Administrative,
        "",
        "\tExample: `.switch-highlvl Upland 100`\r\n" +
        "\tExample: `.switch-highlvl Upland,Ontario 100`",
        "switch-highlvl"
    )]
    public class SwitchHighLevelCommand : ICustomCommand
    {
        private const string SupplyPath = "..\\Accounts\\Accounts - New.txt";
        private const string BannedPath = "..\\Accounts\\Banned Level 30s\\";
        private const string AccountsFile = "high-level_{0}.csv";

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public SwitchHighLevelCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            var city = command.Args[0];
            if (!_config.CityRoles.Contains(city))
            {
                await message.RespondAsync($"{message.Author.Mention} you did not enter a valid city name.");
                return;
            }

            var workers = command.Args[1];
            if (!int.TryParse(workers, out int amount))
            {
                await message.RespondAsync($"{message.Author.Mention} {workers} is not a valid value for an amount.");
                return;
            }

            foreach (var cityName in city.Split(','))
            {
                if (SwitchWorkerAccounts(cityName, amount))
                    await message.RespondAsync($"{message.Author.Mention} switched {amount} high level accounts for {cityName}.");
                else
                    await message.RespondAsync($"{message.Author.Mention} failed to switch high level accounts for {cityName}.");
            }
        }

        #endregion

        #region Private Methods

        private bool SwitchWorkerAccounts(string city, int amount)
        {
            //Check if workers supply file exists.
            var supplyWorkersFile = Path.Combine(_config.MapFolder, SupplyPath);
            if (!File.Exists(supplyWorkersFile))
            {
                _logger.Error($"Failed to find workers supply file...");
                return false;
            }

            //Retrieve all workers in a list.
            var goodWorkers = new List<string>(File.ReadAllLines(supplyWorkersFile));
            if (goodWorkers.Count == 0)
            {
                _logger.Error($"Failed to get list of workers, file is empty...");
                return false;
            }

            //Take an amount from the supply list.
            var count = Math.Min(amount, goodWorkers.Count);
            var newHighLevel = goodWorkers.Take(count).ToList();
            goodWorkers.RemoveAll(newHighLevel.Contains);

            //Write new workers supply list.
            File.WriteAllLines(supplyWorkersFile, goodWorkers);

            //Check if the old workers file for the specified city exists.
            var cityWorkersFile = Path.Combine(_config.MapFolder, string.Format(AccountsFile, city));
            if (!File.Exists(cityWorkersFile))
            {
                _logger.Error($"Failed to get workers file at {cityWorkersFile}.");
                return false;
            }

            //Check if there are any old workers that might be banned, if so write them out.
            var oldWorkers = new List<string>(File.ReadAllLines(cityWorkersFile));
            if (oldWorkers.Count > 0)
            {
                //Write out banned/used workers.
                var bannedWorkersFile = Path.Combine(_config.MapFolder, $"{BannedPath}{DateTime.Now.ToString("yyyy-MM-dd")}.txt");
                File.AppendAllLines(bannedWorkersFile, oldWorkers);
            }

            //Write out good workers.
            File.WriteAllLines(cityWorkersFile, newHighLevel);

            return true;
        }

        #endregion
    }
}