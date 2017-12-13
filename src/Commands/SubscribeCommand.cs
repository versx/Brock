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
        "Subscribe to Pokemon notifications via pokedex number.",
        "\tExample: `.sub 147`\r\n" +
        "\tExample: `.sub 113,242,248`",
        "sub"
    )]
    public class SubscribeCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public SubscribeCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;
            //TODO: If command was from a DM, look through all servers.

            var author = message.Author.Id;
            foreach (var arg in command.Args[0].Split(','))
            {
                var index = Convert.ToUInt32(arg);
                if (!Db.Pokemon.ContainsKey(index.ToString()))
                {
                    await message.RespondAsync($"{index} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = Db.Pokemon[index.ToString()];
                if (!server.ContainsKey(author))
                {
                    server.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon() { PokemonId = index } }, new List<ulong>()));
                    await message.RespondAsync($"{message.Author.Username} has subscribed to {pokemon.Name} notifications!");
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!server[author].Pokemon.Exists(x => x.PokemonId == index))
                    {
                        server[author].Pokemon.Add(new Pokemon() { PokemonId = index /*TODO: Add minimum CP and IV.*/ });
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