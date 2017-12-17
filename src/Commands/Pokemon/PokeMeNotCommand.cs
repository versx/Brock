namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Notifications,
        "Unsubscribe from a one or more or even all subscribed Pokemon notifications.",
        "\tExample: `.pokemenot 149`\r\n" +
        "\tExample: `.pokemenot 3,6,9,147,148,149`\r\n" +
        "\tExample: `.pokemenot` (Removes all subscribed Pokemon notifications.)",
        "pokemenot"
    )]
    public class PokeMeNotCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public PokeMeNotCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;

            if (server.SubscriptionExists(author))
            {
                if (command.HasArgs && command.Args.Count == 1)
                {
                    foreach (var arg in command.Args[0].Split(','))
                    {
                        var index = Convert.ToUInt32(arg);
                        if (!Db.Pokemon.ContainsKey(index.ToString()))
                        {
                            await message.RespondAsync($"Pokedex number {index} is not a valid Pokemon id.");
                            continue;
                        }

                        var pokemon = Db.Pokemon[index.ToString()];
                        var unsubscribePokemon = server[author].Pokemon.Find(x => x.PokemonId == index);
                        if (unsubscribePokemon != null)
                        {
                            if (server[author].Pokemon.Remove(unsubscribePokemon))
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
                    server.RemoveAllPokemon(author);
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