namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using BrockBot.Data;

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
            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;

            if (server.ContainsKey(author))
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

                        if (server[author].PokemonIds.Contains(index))
                        {
                            if (server[author].PokemonIds.Remove(index))
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
                    server.Remove(author);
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