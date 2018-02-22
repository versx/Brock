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
        Categories.Notifications,
        "Subscribe to Pokemon notifications based on the pokedex number or name, minimum IV stats, or minimum level.",
        "\tExample: `.pokeme 147 95`\r\n" +
        "\tExample: `.pokeme pikachu 97`\r\n" +
        "\tExample: `.pokeme 113,242,248 90`\r\n" +
        "\tExample: `.pokeme pikachu,26,129,Dratini 97`\r\n" +
        "\tExample: `.pokeme 113 90 L35` (Supporters Only: Subscribe to Chansey notifications with minimum IV of 90% and minimum level of 32.)\r\n" +
        "\tExample: `.pokeme all 90` (Subscribe to all Pokemon notifications with minimum IV of 90%. Excludes Unown)\r\n" +
        "\tExample: `.pokeme all 90 L30 (Supporters Only: Subscribe to all Pokemon notifications with minimum IV of 90% and minimum level of 30.)",
        "pokeme"
    )]
    public class PokeMeCommand : ICustomCommand
    {
        public const int MaxPokemonSubscriptions = 25;

        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public PokeMeCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (command.Args.Count == 3)
            {
                if (!await _client.IsSupporterOrHigher(message.Author.Id, _config))
                {
                    await message.RespondAsync($"{message.Author.Mention} please provide correct values such as `{_config.CommandsPrefix}{command.Name} 25 97` or `{_config.CommandsPrefix}{command.Name} 25,26,89,90 100`");
                    return;
                }
            }

            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;
            var cmd = command.Args[0];
            //var cpArg = command.Args.Count == 1 ? "0" : command.Args[1];
            var ivArg = command.Args.Count == 1 ? "0" : command.Args[1];
            var lvlArg = command.Args.Count < 3 ? "L0" : command.Args[2];

            //if (!int.TryParse(cpArg, out int cp))
            //{
            //    await message.RespondAsync($"'{cpArg}' is not a valid value for CP.");
            //    return;
            //}

            if (!int.TryParse(ivArg, out int iv))
            {
                await message.RespondAsync($"{message.Author.Mention}, '{ivArg}' is not a valid value for IV.");
                return;
            }

            if (iv < 0 || iv > 100)
            {
                await message.RespondAsync($"{message.Author.Mention}, {iv} must be within the range of 0-100.");
                return;
            }

            if (!int.TryParse(lvlArg.Replace("l", null).Replace("L", null), out int lvl))
            {
                await message.RespondAsync($"{message.Author.Mention}, '{lvlArg}' is not a valid value for Level.");
                return;
            }

            if (lvl < 0 || lvl > 35)
            {
                await message.RespondAsync($"{message.Author.Mention}, {lvl} must be within the range of 0-35.");
                return;
            }

            if (string.Compare(cmd, "all", true) == 0)
            {
                var isSupporter = await _client.IsSupporterOrHigher(author, _config);
                if (!isSupporter)
                {
                    await message.RespondAsync($"{message.Author.Mention} non-supporter members have a limited Pokemon notification amount of {MaxPokemonSubscriptions}, thus you may not use the 'all' parameter. Please narrow down your Pokemon notification subscriptions to be more specific and try again.");
                    return;
                }

                if (iv < 80)
                {
                    await message.RespondAsync($"{message.Author.Mention} may not subscribe to **all** Pokemon with a minimum IV less than 80, please set something higher.");
                    return;
                }
				
				var previousIV = iv;

                for (uint i = 1; i < 386; i++)
                {
                    //Always ignore the user's input for Unown and set it to 0 by default.
                    if (i == 201) iv = 0;

                    var pokemon = _db.Pokemon[i.ToString()];
                    if (!_db.Exists(author))
                    {
                        _db.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon() { PokemonId = i, MinimumIV = iv, MinimumLevel = lvl } }, new List<Pokemon>()));
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!_db[author].Pokemon.Exists(x => x.PokemonId == i))
                        {
                            _db[author].Pokemon.Add(new Pokemon { PokemonId = i, MinimumIV = iv, MinimumLevel = lvl });
                        }
                        else
                        {
                            //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                            var subscribedPokemon = _db[author].Pokemon.Find(x => x.PokemonId == i);
                            if (iv != subscribedPokemon.MinimumIV ||
                                lvl != subscribedPokemon.MinimumLevel)
                            {
                                subscribedPokemon.MinimumIV = iv;
                                subscribedPokemon.MinimumLevel = lvl;
                                continue;
                            }
                        }
                    }
					
					iv = previousIV;
                }

                await message.RespondAsync($"{message.Author.Mention} subscribed to **all** Pokemon notifications with a minimum IV of {iv}%.");
                return;
            }

            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();
            foreach (var arg in cmd.Split(','))
            {
                //TODO: Check if common type pokemon e.g. Pidgey, Ratatta, Spinarak 'they are beneath him and he refuses to discuss them further'

                var pokeId = Helpers.PokemonIdFromName(_db, arg);
                if (pokeId == 0)
                {
                    if (!uint.TryParse(arg, out pokeId))
                    {
                        await message.RespondAsync($"{message.Author.Mention}, failed to lookup Pokemon by name and pokedex id using {arg}.");
                        return;
                    }
                }

                if (IsCommonPokemon(pokeId) && iv < 97)
                {
                    await message.RespondAsync($"{message.Author.Mention} {pokeId} is a common type Pokemon and cannot be subscribed to for notifications unless the IV is set to at least 97% or higher.");
                    continue;
                }

                //var pokeId = Convert.ToUInt32(arg);
                if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, pokedex number {pokeId} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = _db.Pokemon[pokeId.ToString()];
                if (!_db.Exists(author))
                {
                    _db.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon { PokemonId = pokeId, MinimumIV = iv, MinimumLevel = lvl } }, new List<Pokemon>()));
                    subscribed.Add(pokemon.Name);
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!_db[author].Pokemon.Exists(x => x.PokemonId == pokeId))
                    {
                        var isSupporter = await _client.IsSupporterOrHigher(author, _config);
                        if (!isSupporter && _db[author].Pokemon.Count >= MaxPokemonSubscriptions)
                        {
                            await message.RespondAsync($"{message.Author.Mention} non-supporter members have a limited notification amount of {MaxPokemonSubscriptions} different Pokemon, please consider donating to lift this to every Pokemon. Otherwise you will need to remove some subscriptions in order to subscribe to new Pokemon.");
                            return;
                        }

                        _db[author].Pokemon.Add(new Pokemon { PokemonId = pokeId, MinimumIV = iv, MinimumLevel = lvl });
                        subscribed.Add(pokemon.Name);
                    }
                    else
                    {
                        //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                        var subscribedPokemon = _db[author].Pokemon.Find(x => x.PokemonId == pokeId);
                        if (iv != subscribedPokemon.MinimumIV ||
                            lvl != subscribedPokemon.MinimumLevel)
                        {
                            subscribedPokemon.MinimumIV = iv;
                            subscribedPokemon.MinimumLevel = lvl;
                            subscribed.Add(pokemon.Name);
                            continue;
                        }

                        alreadySubscribed.Add(pokemon.Name);
                    }
                }
            }

            await message.RespondAsync
            (
                (subscribed.Count > 0
                    ? $"{message.Author.Mention} has subscribed to **{string.Join("**, **", subscribed)}** notifications with a minimum IV of {iv}%{(command.Args.Count > 2 ? $" and a minimum level of {lvl}" : null)}."
                    : string.Empty) +
                (alreadySubscribed.Count > 0
                    ? $" {message.Author.Mention} is already subscribed to **{string.Join("**, **", alreadySubscribed)}** notifications with a minimum IV of {iv}%{(command.Args.Count > 2 ? $" and a minimum level of {lvl}" : null)}."
                    : string.Empty)
            );
        }

        private bool IsCommonPokemon(uint pokeId)
        {
            var commonPokemon = new List<uint>
            {
                10, //Caterpie
                13, //Weedle
                16, //Pidgey
                17, //Pidgeotto
                19, //Rattata
                20, //Raticate
                21, //Spearow
                23, //Ekans
                27, //Sandshrew
                29, //Nidoran Female
                32, //Nidoran Male
                41, //Zubat
                46, //Paras
                48, //Venonat
                50, //Diglett
                52, //Meowth
                104, //Cubone
                133, //Eevee
                161, //Sentret
                163, //Noothoot
                165, //Ledyba
                167, //Spinarak
                177, //Natu
                187, //Hoppip
                191, //Sunkern
                193, //Yanma
                //194, //Wooper
                198, //Murkrow
                209, //Snubbull
                228, //Houndour
                261, //Poochyena
                263, //Zigzagoon
                265, //Wurmple
                273, //Seedot
                276, //Taillow
                293, //Whismur
                300, //Skitty
                307, //Meditite
                309, //Electrike
                315, //Roselia
                316, //Gulpin
                322, //Numel
                325, //Spoink
                331, //Cacnea
                333, //Swablu
                363, //Spheal

            };
            return commonPokemon.Contains(pokeId);
        }
    }
}