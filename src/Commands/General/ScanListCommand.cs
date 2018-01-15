namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
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
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public ScanListCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var scanList = _config.EncounterList;
            if (scanList.Count == 0)
            {
                await message.RespondAsync("It appears the scan list is empty.");
                return;
            }

            var pokemon = new List<string>();
            foreach (var pkmn in _config.EncounterList)
            {
                if (!Db.Pokemon.ContainsKey(pkmn.ToString())) continue;

                pokemon.Add(Db.Pokemon[pkmn.ToString()].Name);
            }

            await message.RespondAsync("**Current Pokemon CP/IV/Moveset info being scanned for:**\r\n" + string.Join(Environment.NewLine, pokemon));
        }
    }
}