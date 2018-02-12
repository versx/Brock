namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
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
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public InfoCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;
            var isSubbed = _db.Exists(author);
            var hasPokemon = isSubbed && _db[author].Pokemon.Count > 0;
            var hasRaids = isSubbed && _db[author].Raids.Count > 0;
            var msg = string.Empty;

            if (hasPokemon)
            {
                var member = await _client.GetMemberFromUserId(author);
                if (member == null)
                {
                    await message.RespondAsync($"Failed to get discord member from id {author}.");
                    return;
                }

                var feeds = new List<string>();
                foreach (var role in member.Roles)
                {
                    if (_config.CityRoles.Contains(role.Name))
                    {
                        feeds.Add(role.Name);
                    }
                }

                var pokemon = _db[author].Pokemon;
                pokemon.Sort((x, y) => x.PokemonId.CompareTo(y.PokemonId));

                var defaultIV = 0;
                var results = pokemon.GroupBy(p => p.MinimumIV, (key, g) => new { IV = key, Pokes = g.ToList() });
                foreach (var result in results)
                {
                    if (result.Pokes.Count > defaultIV)
                    {
                        defaultIV = result.IV;
                    }
                }

                msg = $"**{message.Author.Mention} Notification Settings:**\r\n";
                msg += $"Enabled: **{(_db[author].Enabled ? "Yes" : "No")}**\r\n";
                msg += $"Feed Zones: **{string.Join("**, **", feeds)}**\r\n";
                msg += $"Pokemon Subscriptions:\r\n```";
                msg += $"Default: **{defaultIV}%** (All unlisted)\r\n";
                foreach (var sub in results)
                {
                    if (sub.IV == defaultIV) continue;

                    foreach (var poke in sub.Pokes)
                    {
                        var pkmn = _db.Pokemon[poke.PokemonId.ToString()];
                        msg += $"{poke.PokemonId}: {pkmn.Name} {poke.MinimumIV}%+{(poke.MinimumLevel > 0 ? $", L{poke.MinimumLevel}+" : null)}\r\n";
                    }
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
                await _client.SendDirectMessage(message.Author, $"**{message.Author.Mention}**'s subscription list is longer than the allowed Discord message character count, here is a partial list:\r\n{msg.Substring(0, Math.Min(msg.Length, 1500))}```", null);
            else
                await _client.SendDirectMessage(message.Author, msg, null);
        }

        private List<string> GetPokemonSubscriptionNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.Exists(userId))
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
            if (_db.Exists(userId))
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