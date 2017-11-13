namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;

    public class SubscribeCommand : ICustomCommand
    {
        private readonly Database _db;

        public bool AdminCommand => false;

        public SubscribeCommand(Database db)
        {
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            //notify <pkmn> <min_cp> <min_iv>
            var author = message.Author.Id;
            foreach (var arg in command.Args[0].Split(','))
            {
                var index = Convert.ToUInt32(arg);
                var pokemon = _db.Pokemon.Find(x => x.Index == index);
                if (pokemon == null)
                {
                    await message.RespondAsync($"Pokedex number {index} is not a valid Pokemon id.");
                    continue;
                }

                if (!server.ContainsKey(author))
                {
                    server.Subscriptions.Add(new Subscription(author, new List<uint> { index }, new List<ulong>()));
                    await message.RespondAsync($"You have successfully subscribed to {pokemon.Name} notifications!");
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!server[author].PokemonIds.Contains(index))
                    {
                        server[author].PokemonIds.Add(index);
                        await message.RespondAsync($"You have successfully subscribed to {pokemon.Name} notifications!");
                    }
                    else
                    {
                        await message.RespondAsync($"You are already subscribed to {pokemon.Name} notifications.");
                    }
                }
            }
        }
    }
}