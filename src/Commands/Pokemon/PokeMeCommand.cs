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
        "Subscribe to Pokemon notifications based on the pokedex number, minimum combat power, and minimum IV stats.",
        "\tExample: `.pokeme 147 0 96`\r\n" +
        "\tExample: `.pokeme 113,242,248 1200 91`",
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
            if (command.Args.Count != 1 || command.Args.Count != 3) return;

            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;
            //TODO: If command was from a DM, look through all servers.

            var author = message.Author.Id;
            var cmd = command.Args[0];
            var cpArg = command.Args.Count == 1 ? "0" : command.Args[1];
            var ivArg = command.Args.Count == 1 ? "0" : command.Args[2];

            if (!int.TryParse(cpArg, out int cp))
            {
                await message.RespondAsync($"{cpArg} is not a valid value for CP.");
                return;
            }

            if (!int.TryParse(ivArg, out int iv))
            {
                await message.RespondAsync($"{ivArg} is not a valid value for IV.");
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
                    server.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon() { PokemonId = index, MinimumCP = cp, MinimumIV = iv } }, new List<Pokemon>()));
                    await message.RespondAsync($"{message.Author.Username} has subscribed to {pokemon.Name} notifications!");
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!server[author].Pokemon.Exists(x => x.PokemonId == index))
                    {
                        server[author].Pokemon.Add(new Pokemon { PokemonId = index, MinimumCP = cp, MinimumIV = iv });
                        await message.RespondAsync($"{message.Author.Username} has subscribed to {pokemon.Name} notifications!");
                    }
                    else
                    {
                        await message.RespondAsync($"{message.Author.Username} is already subscribed to {pokemon.Name} notifications.");
                    }
                }
            }
        }
    }
}