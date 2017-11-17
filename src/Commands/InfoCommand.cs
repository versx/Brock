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
            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            var author = message.Author.Id;
            var isSubbed = server.ContainsKey(author);
            var hasPokemon = isSubbed && server[author].Pokemon.Count > 0;
            var hasChannels = isSubbed && server[author].ChannelIds.Count > 0;
            var msg = string.Empty;

            if (isSubbed)
            {
                if (hasPokemon && hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(server, author))} notifications from channels #{string.Join(", #", await GetChannelNames(server, author))}.";
                }
                else if (hasPokemon && !hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(server, author))} notifications from zero channels.";
                }
                else if (!hasPokemon && hasChannels)
                {
                    msg = $"You are not currently subscribed to any Pokemon notifications from channels #{string.Join(", #", await GetChannelNames(server, author))}.";
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

        private List<string> GetSubscriptionNames(Server server, ulong userId)
        {
            var list = new List<string>();
            if (server.ContainsKey(userId))
            {
                var subscribedPokemon = server[userId].Pokemon;
                subscribedPokemon.Sort();

                foreach (var poke in subscribedPokemon)
                {
                    var pokemon = _db.Pokemon.Find(x => x.Id == poke.PokemonId);
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }

        private async Task<List<string>> GetChannelNames(Server server, ulong userId)
        {
            var list = new List<string>();
            if (server.ContainsKey(userId))
            {
                foreach (var channelId in server[userId].ChannelIds)
                {
                    var channel = await _client.GetChannel(channelId);
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