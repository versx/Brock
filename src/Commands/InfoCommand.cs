namespace PokeFilterBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;

    public class InfoCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public InfoCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            var author = message.Author.Id;
            var isSubbed = _db.Subscriptions.ContainsKey(author);
            var hasPokemon = isSubbed && _db.Subscriptions[author].PokemonIds.Count > 0;
            var hasChannels = isSubbed && _db.Subscriptions[author].Channels.Count > 0;
            var msg = string.Empty;

            if (isSubbed)
            {
                if (hasPokemon && hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(author))} notifications from channels #{string.Join(", #", GetChannelNames(author))}.";
                }
                else if (hasPokemon && !hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(author))} notifications from zero channels.";
                }
                else if (!hasPokemon && hasChannels)
                {
                    msg = $"You are not currently subscribed to any Pokemon notifications from channels #{string.Join(", #", GetChannelNames(author))}.";
                }
                else if (!hasPokemon && !hasChannels)
                {
                    msg = "You are not currently subscribed to any Pokemon notifications from any channels.";
                }
            }
            else
            {
                msg = "You are not subscribed to any Pokemon.";
            }

            await message.RespondAsync(msg);
        }

        private List<string> GetSubscriptionNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.Subscriptions.ContainsKey(userId))
            {
                var pokeIds = _db.Subscriptions[userId].PokemonIds;
                pokeIds.Sort();

                foreach (uint id in pokeIds)
                {
                    var pokemon = _db.Pokemon.Find(x => x.Index == id);
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }

        private async Task<List<string>> GetChannelNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.Subscriptions.ContainsKey(userId))
            {
                foreach (var channelId in _db.Subscriptions[userId].Channels)
                {
                    var channel = await _client.GetChannelAsync(channelId);
                    if (channel != null)
                    {
                        list.Add(channel.Name);
                    }
                }
            }
            return list;
        }
    }
}