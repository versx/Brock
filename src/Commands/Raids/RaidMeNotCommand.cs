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
        "\tExample: `.raidmenot Absol`\r\n" +
        "\tExample: `.raidmenot Tyranitar,Snorlax`\r\n" +
        "\tExample: `.raidmenot` (Removes all subscribed Pokemon notifications.)",
        "raidmenot"
    )]
    public class RaidMeNotCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public RaidMeNotCommand(DiscordClient client, IDatabase db)
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
                    var cmd = command.Args[0];
                    foreach (var arg in cmd.Split(','))
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
                        var unsubscribePokemon = server[author].Pokemon.Find(x => x.PokemonId == pokeIndex);
                        if (unsubscribePokemon != null)
                        {
                            if (server[author].Pokemon.Remove(unsubscribePokemon))
                            {
                                await message.RespondAsync($"You have successfully unsubscribed from {pokemon.Name} raid notifications!");
                            }
                        }
                        else
                        {
                            await message.RespondAsync($"You are not subscribed to {pokemon.Name} raid notifications.");
                        }
                    }
                }
                else
                {
                    server.RemoveAllRaids(author);
                    await message.RespondAsync($"You have successfully unsubscribed from all raid notifications!");
                }
            }
            else
            {
                await message.RespondAsync($"You are not subscribed to any raid notifications.");
            }
        }
    }
}