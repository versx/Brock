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

    [Command(
        Categories.General,
        "Provides a list of Pokemon whose CP/IV information is being scanned for.",
        "\tExample: `.scanlist`",
        "scanlist"
    )]
    public class ScanListCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public ScanListCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs)
            {
                await message.RespondAsync($"{message.Author.Mention} please specify a city.");
                return;
            }

            var city = command.Args[0];
            if (!_config.CityRoles.Exists(x => string.Compare(x, city, true) == 0))
            {
                await message.RespondAsync($"{message.Author.Mention} you've specified an invalid city name.");
                return;
            }

            var encounterListFilePath = Path.Combine(SetEncounterListCommand.MapPath, $"enc-whitelist-rares-{city}.txt");
            if (!File.Exists(encounterListFilePath))
            {
                await message.RespondAsync($"{message.Author.Mention} the specified encounter list does not exist.");
                return;
            }

            var scanList = new List<string>(File.ReadAllLines(encounterListFilePath));
            if (scanList.Count == 0)
            {
                await message.RespondAsync($"{message.Author.Mention} it appears the scan list is empty.");
                return;
            }

            scanList.Sort((x, y) => Convert.ToInt32(x).CompareTo(Convert.ToInt32(y)));
            var pokemon = new List<string>();

            foreach (var pkmn in scanList)
            {
                if (!_db.Pokemon.ContainsKey(pkmn)) continue;

                pokemon.Add(_db.Pokemon[pkmn].Name);
            }

            await message.RespondAsync($"**{city} Pokemon CP/IV/Moveset Scan List:**\r\n```{string.Join(Environment.NewLine, pokemon)}```");
        }
    }
}