namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    //TODO: If list is longer than x, look for most occurred IV%--use that as 'Default' then list others.

    [Command(
        Categories.Notifications, 
        "Shows your current Pokemon and Raid notification subscriptions.",
        "\tExample: `.info`", 
        "info"
    )]
    public class InfoCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public InfoCommand(DiscordClient client, IDatabase db, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;
            var isSubbed = _db.SubscriptionExists(author);
            var hasPokemon = isSubbed && _db[author].Pokemon.Count > 0;
            var hasRaids = isSubbed && _db[author].Raids.Count > 0;
            var msg = string.Empty;

            if (hasPokemon)
            {
                var pokemon = _db[author].Pokemon;
                pokemon.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                msg = $"**{message.Author.Username} Notification Settings:**\r\n";
                msg += $"Enabled: **{(_db[author].Enabled ? "Yes" : "No")}**\r\n";
                msg += $"Pokemon Subscriptions:\r\n```";

                foreach (var sub in pokemon)
                {
                    if (!_db.Pokemon.ContainsKey(sub.PokemonId.ToString()))
                    {
                        _logger.Error($"Failed to find Pokemon with id {sub.PokemonId} in the Pokemon database, skipping...");
                        continue;
                    }

                    var pkmn = _db.Pokemon[sub.PokemonId.ToString()];
                    msg += $"{sub.PokemonId}: {pkmn.Name} {sub.MinimumIV}%+\r\n";
                }
                msg += "```" + Environment.NewLine + Environment.NewLine;
            }

            if (hasRaids)
            {
                msg += $"Raid Subscriptions:\r\n```";
                msg += string.Join(", ", GetRaidSubscriptionNames(author));
				msg += "```";
            }

            if (string.IsNullOrEmpty(msg))
            {
                msg = $"**{message.Author.Mention}** is not subscribed to any Pokemon or Raid notifications.";
            }

            if (msg.Length > 2000)
                await _client.SendDirectMessage(message.Author, $"**{message.Author.Mention}**'s subscription list is longer than the allowed Discord message character count, here is a partial list:\r\n{msg.Substring(0, Math.Min(msg.Length, 1500))}", null);
            else
                await _client.SendDirectMessage(message.Author, msg, null);
        }

        private List<string> GetPokemonSubscriptionNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.SubscriptionExists(userId))
            {
                var subscribedPokemon = _db[userId].Pokemon;
                subscribedPokemon.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                foreach (var poke in subscribedPokemon)
                {
                    if (!_db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;

                    var pokemon = _db.Pokemon[poke.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }

        private List<string> GetRaidSubscriptionNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.SubscriptionExists(userId))
            {
                var subscribedRaids = _db[userId].Raids;
                subscribedRaids.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                foreach (var poke in subscribedRaids)
                {
                    if (!_db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;

                    var pokemon = _db.Pokemon[poke.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }
    }
}