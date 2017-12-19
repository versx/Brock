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
        "Shows your current notification subscriptions.",
        "\tExample: `.info`", 
        "info"
    )]
    public class InfoCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public InfoCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            var server = Db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;
            var isSubbed = server.SubscriptionExists(author);
            var hasPokemon = isSubbed && server[author].Pokemon.Count > 0;
            var hasRaids = isSubbed && server[author].Raids.Count > 0;
            var msg = string.Empty;

            if (hasPokemon && hasRaids)
            {
                msg = $"**{message.Author.Username}** is subscribed to **{string.Join(", ", GetPokemonSubscriptionNames(server, author))}** Pokemon notifications.\r\n**{message.Author.Username}** is subscribed to **{string.Join(", ", GetRaidSubscriptionNames(server, author))}** Raid notifications.";
            }
            else if (hasPokemon && !hasRaids)
            {
                msg = $"**{message.Author.Username}** is currently subscribed to **{string.Join(", ", GetPokemonSubscriptionNames(server, author))}** Pokemon notifications and 0 Raid notifications.";
            }
            else if (!hasPokemon && hasRaids)
            {
                msg = $"**{message.Author.Username}** is currently subscribed to 0 Pokemon notifications but subscribed to **{string.Join(", ",GetRaidSubscriptionNames(server, author))}** Raid notifications.";
            }
            else if (!hasPokemon && !hasRaids)
            {
                msg = $"**{message.Author.Username}** is not currently subscribed to any Pokemon or Raid notifications.";
            }
            
            await message.RespondAsync(msg);
        }

        private List<string> GetPokemonSubscriptionNames(Server server, ulong userId)
        {
            var list = new List<string>();
            if (server.SubscriptionExists(userId))
            {
                var subscribedPokemon = server[userId].Pokemon;
                subscribedPokemon.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));
                //subscribedPokemon.Sort();

                foreach (var poke in subscribedPokemon)
                {
                    if (!Db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;

                    var pokemon = Db.Pokemon[poke.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }

        private List<string> GetRaidSubscriptionNames(Server server, ulong userId)
        {
            var list = new List<string>();
            if (server.SubscriptionExists(userId))
            {
                var subscribedRaids = server[userId].Raids;
                subscribedRaids.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                foreach (var poke in subscribedRaids)
                {
                    if (!Db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;

                    var pokemon = Db.Pokemon[poke.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }
    }
}