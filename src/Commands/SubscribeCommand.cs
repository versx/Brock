namespace PokeFilterBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using PokeFilterBot.Data;

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
            //notify <pkmn> <min_cp> <min_iv>
            var author = message.Author.Id;
            if (command.HasArgs && command.Args.Count == 1)
            {
                foreach (var arg in command.Args[0].Split(','))
                {
                    var index = Convert.ToUInt32(arg);
                    var pokemon = _db.Pokemon.Find(x => x.Index == index);
                    if (pokemon == null)
                    {
                        await message.RespondAsync($"Pokedex number {index} is not a valid Pokemon id.");
                        continue;
                    }

                    if (!_db.Subscriptions.ContainsKey(author))
                    {
                        _db.Subscriptions.Add(new Subscription(author, new List<uint> { index }, new List<string>()));
                        await message.RespondAsync($"You have successfully subscribed to {pokemon.Name} notifications!");
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!_db.Subscriptions[author].PokemonIds.Contains(index))
                        {
                            _db.Subscriptions[author].PokemonIds.Add(index);
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
}