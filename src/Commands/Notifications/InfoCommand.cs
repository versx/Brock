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
    using BrockBot.Utilities;

    [Command(
        Categories.Notifications, 
        "Shows your current Pokemon and Raid boss notification subscriptions.",
        "\tExample: `.info`", 
        "info"
    )]
    public class InfoCommand : ICustomCommand
    {
        private const int MaxPokemonDisplayed = 70;

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

            if (command.HasArgs && command.Args.Count == 1)
            {
                if (!message.Author.Id.IsModeratorOrHigher(_config))
                {
                    await message.RespondAsync($"{message.Author.Mention} is not a moderator or higher thus you may not see other's subscription settings.");
                    return;
                }

                var mention = command.Args[0];
                var userId = DiscordHelpers.ConvertMentionToUserId(mention);
                if (userId <= 0)
                {
                    await message.RespondAsync($"{message.Author.Mention} failed to retrieve user with mention tag {mention}.");
                    return;
                }

                await SendUserSubscriptionSettings(message.Author, userId);
                return;
            }

            await SendUserSubscriptionSettings(message.Author, message.Author.Id);
        }

        private async Task SendUserSubscriptionSettings(DiscordUser receiver, ulong userId)
        {
            var discordUser = await _client.GetUser(userId);
            if (discordUser == null)
            {
                _logger.Error($"Failed to retreive user with id {userId}.");
                return;
            }

            var userSettings = await BuildUserSubscriptionSettings(discordUser);

            if (userSettings.Length > 2000)
                await _client.SendDirectMessage(receiver, $"**{discordUser.Mention}**'s subscription list is longer than the allowed Discord message character count, here is a partial list:\r\n{userSettings.Substring(0, Math.Min(userSettings.Length, 1500))}```", null);
            else
                await _client.SendDirectMessage(receiver, userSettings, null);
        }

        private async Task<string> BuildUserSubscriptionSettings(DiscordUser user)
        {
            var author = user.Id;
            var isSubbed = _db.Exists(author);
            var hasPokemon = isSubbed && _db[author].Pokemon.Count > 0;
            var hasRaids = isSubbed && _db[author].Raids.Count > 0;
            var msg = string.Empty;

            if (hasPokemon)
            {
                var member = await _client.GetMemberFromUserId(author);
                if (member == null)
                {
                    return $"Failed to get discord member from id {author}.";
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

                var exceedsLimits = pokemon.Count > MaxPokemonDisplayed;
                var defaultIV = 0;
                var results = pokemon.GroupBy(p => p.MinimumIV, (key, g) => new { IV = key, Pokes = g.ToList() });
                foreach (var result in results)
                {
                    if (result.Pokes.Count > defaultIV)
                    {
                        defaultIV = result.IV;
                    }
                }

                msg = $"**{user.Mention} Notification Settings:**\r\n";
                msg += $"Enabled: **{(_db[author].Enabled ? "Yes" : "No")}**\r\n";
                msg += $"Feed Zones: **{string.Join("**, **", feeds)}**\r\n";
                msg += $"Pokemon Subscriptions:\r\n```";

                if (exceedsLimits)
                {
                    msg += $"Default: {defaultIV}% (All unlisted)\r\n";
                }

                foreach (var sub in results)
                {
                    if (sub.IV == defaultIV && exceedsLimits) continue;

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
                msg = $"**{user.Mention}** is not subscribed to any Pokemon or Raid notifications.";
            }

            return msg;
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