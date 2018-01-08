namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Extensions;

    [Command(
        Categories.Notifications,
        "Subscribe to Pokemon notifications based on the pokedex number and minimum IV stats.",
        "\tExample: `.pokeme 147 95`\r\n" +
        "\tExample: `.pokeme 113,242,248 90`\r\n" +
        "\tExample: `.pokeme all 90` (Subscribe to all Pokemon notifications with minimum IV of 90%.)\r\n" +
        "\tExample: `.pokeme * 90` (Subscribe to all Pokemon notifications with minimum IV of 90%.)\r\n",
        "pokeme"
    )]
    public class PokeMeCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public PokeMeCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count > 2) return;

            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;
            //TODO: If command was from a DM, look through all servers.

            var author = message.Author.Id;
            var cmd = command.Args[0];
            //var cpArg = command.Args.Count == 1 ? "0" : command.Args[1];
            var ivArg = command.Args.Count == 1 ? "0" : command.Args[1];

            //if (!int.TryParse(cpArg, out int cp))
            //{
            //    await message.RespondAsync($"'{cpArg}' is not a valid value for CP.");
            //    return;
            //}

            if (!int.TryParse(ivArg, out int iv))
            {
                await message.RespondAsync($"'{ivArg}' is not a valid value for IV.");
                return;
            }

            var alreadySubscribed = new List<string>();
            var subscribed = new List<string>();

            if (cmd == "*" || string.Compare(cmd.ToLower(), "all", true) == 0)
            {
                for (uint i = 1; i < 390; i++)
                {
                    var pokemon = Db.Pokemon[i.ToString()];
                    if (!server.SubscriptionExists(author))
                    {
                        server.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon() { PokemonId = i, /*MinimumCP = cp,*/ MinimumIV = iv } }, new List<Pokemon>()));
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!server[author].Pokemon.Exists(x => x.PokemonId == i))
                        {
                            server[author].Pokemon.Add(new Pokemon { PokemonId = i, MinimumIV = iv });
                            subscribed.Add(pokemon.Name);
                        }
                        else
                        {
                            //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                            var subscribedPokemon = server[author].Pokemon.Find(x => x.PokemonId == i);
                            if (iv != subscribedPokemon.MinimumIV)
                            {
                                subscribedPokemon.MinimumIV = iv;
                                continue;
                            }

                            alreadySubscribed.Add(pokemon.Name);
                        }
                    }
                }

                await message.RespondAsync($"{message.Author.Username} subscribed to all Pokemon notifications with a minimum IV of {iv}%.");
                return;
            }

            foreach (var arg in cmd.Split(','))
            {
                var index = Convert.ToUInt32(arg);
                if (!Db.Pokemon.ContainsKey(index.ToString()))
                {
                    await message.RespondAsync($"{index} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = Db.Pokemon[index.ToString()];
                if (!server.SubscriptionExists(author))
                {
                    server.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon { PokemonId = index, /*MinimumCP = cp,*/ MinimumIV = iv } }, new List<Pokemon>()));
                    subscribed.Add(pokemon.Name);
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!server[author].Pokemon.Exists(x => x.PokemonId == index))
                    {
                        server[author].Pokemon.Add(new Pokemon { PokemonId = index, MinimumIV = iv });
                        subscribed.Add(pokemon.Name);
                    }
                    else
                    {
                        //Check if minimum IV value is different from value in database, if not add it to the already subscribed list.
                        var subscribedPokemon = server[author].Pokemon.Find(x => x.PokemonId == index);
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
                    ? $"{message.Author.Username} has subscribed to **{string.Join("**, **", subscribed)}** notifications with a minimum IV of {iv}%."
                    : string.Empty) +
                (alreadySubscribed.Count > 0
                    ? $" {message.Author.Username} is already subscribed to **{string.Join("**, **", alreadySubscribed)}** notifications."
                    : string.Empty)
            );
        }
    }
}