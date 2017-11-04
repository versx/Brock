namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using PokeFilterBot.Data;

    public class UnsubscribeCommand : ICustomCommand
    {
        private readonly Database _db;

        public bool AdminCommand => false;

        public UnsubscribeCommand(Database db)
        {
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            var author = message.Author.Id;

            if (_db.Subscriptions.ContainsKey(author))
            {
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

                        if (_db.Subscriptions[author].PokemonIds.Contains(index))
                        {
                            if (_db.Subscriptions[author].PokemonIds.Remove(index))
                            {
                                await message.RespondAsync($"You have successfully unsubscribed from {pokemon.Name} notifications!");
                            }
                        }
                        else
                        {
                            await message.RespondAsync($"You are not subscribed to {pokemon.Name} notifications.");
                        }
                    }
                }
                else
                {
                    _db.Subscriptions.Remove(author);
                    await message.RespondAsync($"You have successfully unsubscribed from all notifications!");
                }
            }
            else
            {
                await message.RespondAsync($"You are not subscribed to any notifications.");
            }
        }
    }
}