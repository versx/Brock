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
        "Subscribe to Pokemon raid notifications.",
        "\tExample: `.raidme Absol`\r\n" +
        "\tExample: `.raidme Tyranitar,Magikarp`",
        "raidme"
    )]
    public class RaidMeCommand: ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public RaidMeCommand(DiscordClient client, IDatabase db)
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

            var author = message.Author.Id;
            foreach (var arg in command.Args[0].Split(','))
            {
                var exists = false;
                var pokeIndex = 0u;
                foreach (var p in Db.Pokemon)
                {
                    if (p.Value.Color.Contains(arg))
                    {
                        exists = true;
                        pokeIndex = Convert.ToUInt32(p.Key);
                    }
                }

                if (!exists)
                {
                    await message.RespondAsync($"Failed to find Pokemon {arg}.");
                    return;
                }

                var pokemon = Db.Pokemon[pokeIndex.ToString()];
                if (!server.SubscriptionExists(author))
                {
                    server.Subscriptions.Add(new Subscription<Pokemon>(author, new List<Pokemon> { new Pokemon { PokemonId = pokeIndex } }, new List<Pokemon>()));
                    await message.RespondAsync($"{message.Author.Username} has subscribed to {pokemon.Name} raid notifications!");
                }
                else
                {
                    var subs = server[author];

                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!subs.Raids.Exists(x => x.PokemonId == pokeIndex))
                    {
                        subs.Raids.Add(new Pokemon { PokemonId = pokeIndex });
                        await message.RespondAsync($"{message.Author.Username} has subscribed to {pokemon.Name} raid notifications!");
                    }
                    else
                    {
                        await message.RespondAsync($"{message.Author.Username} is already subscribed to {pokemon.Name} raid notifications.");
                    }
                }
            }
        }
    }
}