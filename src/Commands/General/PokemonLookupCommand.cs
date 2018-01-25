namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Net;

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
        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public PokemonLookupCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var cmd = command.Args[0];
            PokemonInfo pkmn = null;

            if (!int.TryParse(cmd, out int pokeId))
            {
                Console.WriteLine($"Failed to parse Pokemon index {cmd}, searching by Pokemon name now...");

                foreach (var poke in Db.Pokemon)
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
                if (!Db.Pokemon.ContainsKey(pokeId.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to lookup Pokemon with id {pokeId}.");
                    return;
                }

                pkmn = Db.Pokemon[pokeId.ToString()];
            }

            var types = pkmn.Types.Count > 1 ? pkmn.Types[0].Type + "/" + pkmn.Types[1].Type : pkmn.Types[0].Type;
            var evolutions = (pkmn.Evolutions == null || pkmn.Evolutions.Count == 0 ? string.Empty : string.Join(", ", pkmn.Evolutions));

            var eb = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor
                {
                    Name = $"{pkmn.Name} (Id: {pokeId}, Gen: {pkmn.BaseStats.Generation}{(pkmn.BaseStats.Legendary ? " Legendary" : "")})"
                }
            };
            //eb.AddField(pkmn.Name, $"ID: {pokeId}, Gen: {pkmn.BaseStats.Generation}{(pkmn.BaseStats.Legendary ? " Legendary" : "")}", true);
            eb.AddField("IV Statistics:", $"Atk: {pkmn.BaseStats.Attack}, Def: {pkmn.BaseStats.Defense}, Sta: {pkmn.BaseStats.Stamina}", true);
            if (!string.IsNullOrEmpty(pkmn.Rarity))
            {
                eb.AddField("Rarity:", pkmn.Rarity, true);
            }
            if (!string.IsNullOrEmpty(pkmn.SpawnRate))
            {
                eb.AddField("Spawn Rate:", pkmn.SpawnRate, true);
            }
            eb.AddField("Gender Ratio:", $"{pkmn.GenderRatio.Male}% Male/{pkmn.GenderRatio.Female}% Female", true);
            if (!string.IsNullOrEmpty(evolutions))
            {
                eb.AddField("Evolutions:", evolutions, true);
            }
            eb.AddField("Type:", types, true);

            var strengths = new List<string>();
            var weaknesses = new List<string>();
            foreach (var type in types.Split('/'))
            {
                foreach (var strength in Helpers.GetStrengths(type))
                {
                    if (!strengths.Contains(strength))
                    {
                        strengths.Add(strength);
                    }
                }
                foreach (var weakness in Helpers.GetWeaknesses(type))
                {
                    if (!weaknesses.Contains(weakness))
                    {
                        weaknesses.Add(weakness);
                    }
                }
            }

            if (strengths.Count > 0)
            {
                eb.AddField("Strengths:", string.Join(", ", strengths));
            }

            if (weaknesses.Count > 0)
            {
                eb.AddField("Weaknesses:", string.Join(", ", weaknesses));
            }

            eb.ImageUrl = string.Format(HttpServer.PokemonImage, pokeId);
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}