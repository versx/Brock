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
        "Subscribe to Pokemon notifications based on the pokedex number and minimum IV stats.",
        "\tExample: `.pokeme 147 95`\r\n" +
        "\tExample: `.pokeme 113,242,248 90`\r\n" +
        "\tExample: `.pokeme all 90` (Subscribe to all Pokemon notifications with minimum IV of 90%. Excludes Unown)\r\n" +
        "\tExample: `.pokeme * 90` (Subscribe to all Pokemon notifications with minimum IV of 90%. Excludes Unown)\r\n",
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
            if (command.Args.Count > 2)
            {
                await message.RespondAsync($"{message.Author.Mention} please provide correct values such as `{_config.CommandsPrefix}{command.Name} 25 97`, `{_config.CommandsPrefix}{command.Name} 25,26,89,90 100`");
                return;
            }

            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;
            var cmd = command.Args[0];
            //var cpArg = command.Args.Count == 1 ? "0" : command.Args[1];
            var ivArg = command.Args.Count == 1 ? "0" : command.Args[1];
            //var lvlArg = command.Args.Count == 1 ? "0" : command.Args[2];

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

            //if (!int.TryParse(lvlArg, out int lvl))
            //{
            //    await message.RespondAsync($"{message.Author.Mention}, '{lvlArg}' is not a valid value for Level.");
            //    return;
            //}

            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();

            if (cmd == "*" || string.Compare(cmd.ToLower(), "all", true) == 0)
            {
                var isSupporter = await _client.IsSupporterOrHigher(author, _config);
                if (!isSupporter)
                {
                    await message.RespondAsync($"{message.Author.Mention} non-supporter members have a limited notification amount of {MaxPokemonSubscriptions}, thus you may not use the 'all' parameter. Please narrow down your notification subscriptions to be more specific and try again.");
                    return;
                }

                if (iv < 80)
                {
                    await message.RespondAsync($"{message.Author.Mention} may not subscribe to all Pokemon with a minimum IV less than 80, please set something higher.");
                    return;
                }
				
				var previousIV = iv;

                for (uint i = 1; i < 390; i++)
                {
                    //Always ignore the user's input for Unown and set it to 0 by default.
                    if (i == 201) iv = 0;

                    var pokemon = _db.Pokemon[i.ToString()];
                    if (!_db.SubscriptionExists(author))
                    {
                        _db.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon() { PokemonId = i, /*MinimumCP = cp,*/ MinimumIV = iv } }, new List<Pokemon>()));
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!_db[author].Pokemon.Exists(x => x.PokemonId == i))
                        {
                            _db[author].Pokemon.Add(new Pokemon { PokemonId = i, MinimumIV = iv });
                            subscribed.Add(pokemon.Name);
                        }
                        else
                        {
                            //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                            var subscribedPokemon = _db[author].Pokemon.Find(x => x.PokemonId == i);
                            if (iv != subscribedPokemon.MinimumIV)
                            {
                                subscribedPokemon.MinimumIV = iv;
                                continue;
                            }

                            alreadySubscribed.Add(pokemon.Name);
                        }
                    }
					
					iv = previousIV;
                }

                await message.RespondAsync($"{message.Author.Mention} subscribed to all Pokemon notifications with a minimum IV of {iv}%.");
                return;
            }

            foreach (var arg in cmd.Split(','))
            {
                var pokeId = Convert.ToUInt32(arg);
                if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, pokedex number {pokeId} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = _db.Pokemon[pokeId.ToString()];
                if (!_db.SubscriptionExists(author))
                {
                    _db.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon { PokemonId = pokeId, /*MinimumCP = cp,*/ MinimumIV = iv } }, new List<Pokemon>()));
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

                        _db[author].Pokemon.Add(new Pokemon { PokemonId = pokeId, MinimumIV = iv });
                        subscribed.Add(pokemon.Name);
                    }
                    else
                    {
                        //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                        var subscribedPokemon = _db[author].Pokemon.Find(x => x.PokemonId == pokeId);
                        if (iv != subscribedPokemon.MinimumIV)
                        {
                            subscribedPokemon.MinimumIV = iv;
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
                    ? $"{message.Author.Mention} has subscribed to **{string.Join("**, **", subscribed)}** notifications with a minimum IV of {iv}%."
                    : string.Empty) +
                (alreadySubscribed.Count > 0
                    ? $" {message.Author.Mention} is already subscribed to **{string.Join("**, **", alreadySubscribed)}** notifications with a minimum IV of {iv}%."
                    : string.Empty)
            );
        }
    }
}