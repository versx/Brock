namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Extensions;

    [Command(
        Categories.General,
        "Simple Pokemon stats lookup.",
        "\tExample: `.search 25`\r\n" +
        "\tExample: `.search larvi`\r\n" +
        "\tExample: `.search mewtwo`",
        "search"
    )]
    public class PokemonLookupCommand : ICustomCommand
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public PokemonLookupCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        #endregion

        #region Public Methods

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var cmd = command.Args[0];
            PokemonInfo pkmn = null;

            if (!int.TryParse(cmd, out int pokeId))
            {
                foreach (var poke in _db.Pokemon)
                {
                    if (poke.Value.Name.ToLower().Contains(cmd))
                    {
                        pokeId = Convert.ToInt32(poke.Key);
                        pkmn = poke.Value;
                        break;
                    }
                }
            }
            else
            {
                if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to lookup Pokemon with id {pokeId}.");
                    return;
                }

                pkmn = _db.Pokemon[pokeId.ToString()];
            }

            if (pkmn == null)
            {
                await message.RespondAsync($"{message.Author.Mention}, failed to lookup Pokemon '{cmd}'.");
                return;
            }

            var types = pkmn.Types.Count > 1 ? pkmn.Types[0].Type + "/" + pkmn.Types[1].Type : pkmn.Types[0].Type;
            var evolutions = (pkmn.Evolutions == null || pkmn.Evolutions.Count == 0 ? string.Empty : string.Join(", ", pkmn.Evolutions));

            var text = string.Empty;
            //var eb = new DiscordEmbedBuilder
            //{
            //    Author = new DiscordEmbedBuilder.EmbedAuthor
            //    {
            //        Name = $"{pkmn.Name} (Id: {pokeId}, Gen: {pkmn.BaseStats.Generation}{(pkmn.BaseStats.Legendary ? " Legendary" : "")})"
            //    }
            //};
            //eb.AddField(pkmn.Name, $"ID: {pokeId}, Gen: {pkmn.BaseStats.Generation}{(pkmn.BaseStats.Legendary ? " Legendary" : "")}", true);
            text += $"**{pkmn.Name}** (Id: {pokeId}, Gen {pkmn.BaseStats.Generation}{(pkmn.BaseStats.Legendary ? " Legendary" : "")})\r\n";

            if (Convert.ToUInt32(pokeId).IsValidRaidBoss(_config.RaidBosses))
            {
                var perfectRange = _db.GetPokemonCpRange(pokeId, 20);
                var boostedRange = _db.GetPokemonCpRange(pokeId, 25);
                text += $"**Perfect CP:** {perfectRange.Best.ToString("N0")} / :white_sun_rain_cloud: {boostedRange.Best.ToString("N0")}\r\n";
            }

            //eb.AddField("Base Stats:", $"Atk: {pkmn.BaseStats.Attack}, Def: {pkmn.BaseStats.Defense}, Sta: {pkmn.BaseStats.Stamina}", true);
            text += $"**Base Stats:** Atk: {pkmn.BaseStats.Attack}, Def: {pkmn.BaseStats.Defense}, Sta: {pkmn.BaseStats.Stamina}\r\n";
            if (!string.IsNullOrEmpty(pkmn.Rarity))
            {
                //eb.AddField("Rarity:", pkmn.Rarity, true);
                text += $"**Rarity:** {pkmn.Rarity}\r\n";
            }
            if (!string.IsNullOrEmpty(pkmn.SpawnRate))
            {
                //eb.AddField("Spawn Rate:", pkmn.SpawnRate, true);
                text += $"**Spawn Rate:** {pkmn.SpawnRate}\r\n";
            }
            //eb.AddField("Gender Ratio:", $"{Math.Round(pkmn.GenderRatio.Male * 100, 2)}% Male/{Math.Round(pkmn.GenderRatio.Female * 100, 2)}% Female", true);
            text += $"**Gender Ratio:** {Math.Round(pkmn.GenderRatio.Male * 100, 2)}% Male/{Math.Round(pkmn.GenderRatio.Female * 100, 2)}% Female\r\n";
            if (!string.IsNullOrEmpty(evolutions))
            {
                //eb.AddField("Evolutions:", evolutions, true);
                text += $"**Evolutions:** {evolutions}\r\n";
            }
            //eb.AddField("Type:", types, true);
            text += $"**Types:** {types}\r\n";

            var strengths = new List<string>();
            var weaknesses = new List<string>();
            foreach (var type in types.Split('/'))
            {
                foreach (var strength in PokemonExtensions.GetStrengths(type))
                {
                    if (!strengths.Contains(strength.ToLower()))
                    {
                        strengths.Add(strength.ToLower());
                    }
                }
                foreach (var weakness in PokemonExtensions.GetWeaknesses(type))
                {
                    if (!weaknesses.Contains(weakness.ToLower()))
                    {
                        weaknesses.Add(weakness.ToLower());
                    }
                }
            }

            if (strengths.Count > 0)
            {
                text += $"**Strong Against:** :types_{string.Join(": :types_", strengths)}:\r\n";
                //eb.AddField("Strong Against:", ":types_" + string.Join(": :types_", strengths) + ":");
            }

            if (weaknesses.Count > 0)
            {
                //eb.AddField("Weaknesses:", ":types_" + string.Join(": :types_", weaknesses) + ":");
                text += $"**Weaknesses:** :types_{string.Join(": :types_", weaknesses)}:\r\n";
            }

            //eb.ImageUrl = string.Format(Strings.PokemonImage, pokeId, 0);
            //var embed = eb.Build();

            //await message.RespondAsync(test, false, embed);
            await message.RespondAsync(text);
        }

        #endregion
    }
}