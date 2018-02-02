﻿namespace BrockBot.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Utilities;

    public class NotificationProcessor
    {
        #region Constants

        public const int NotificationTimeout = 10;
        public const int MaxNotificationsPerDayNormal = 25;
        public const int MaxNotificationsPerDaySupporter = 100;

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Constructor

        public NotificationProcessor(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task ProcessPokemon(PokemonData pkmn)
        {
            if (_db == null)
            {
                _logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                var user = _db.Subscriptions[i];
                if (!user.Enabled) continue;

                discordUser = await _client.GetUser(user.UserId);
                if (discordUser == null) continue;

                if (!user.Pokemon.Exists(x => x.PokemonId == pkmn.PokemonId)) continue;
                var subscribedPokemon = user.Pokemon.Find(x => x.PokemonId == pkmn.PokemonId);

                if (!_db.Pokemon.ContainsKey(subscribedPokemon.PokemonId.ToString())) continue;
                var pokemon = _db.Pokemon[subscribedPokemon.PokemonId.ToString()];
                if (pokemon == null) continue;

                var matchesIV = MatchesIvFilter(pkmn.IV, subscribedPokemon.MinimumIV);
                //var matchesCP = MatchesCpFilter(pkmn.CP, subscribedPokemon.MinimumCP);
                //var matchesLvl = MatchesLvlFilter(pkmn.PlayerLevel, subscribedPokemon.MinimumLevel);

                //if (!(matchesIV || matchesCP || matchesLvl)) continue;
                if (!matchesIV) continue;

                _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} CP{pkmn.CP} {pkmn.IV} IV L{pkmn.PlayerLevel} has spawned...");

                var embed = await BuildEmbedPokemon(pkmn, user.UserId);
                if (embed == null) continue;

                if (await CheckIfExceededNotificationLimit(user)) return;

                user.NotificationsToday++;

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(NotificationTimeout);
            }
        }

        public async Task ProcessRaid(RaidData raid)
        {
            if (_db == null)
            {
                _logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                var user = _db.Subscriptions[i];
                if (!user.Enabled) continue;

                discordUser = await _client.GetUser(user.UserId);
                if (discordUser == null) continue;

                if (!user.Raids.Exists(x => x.PokemonId == raid.PokemonId)) continue;
                var subscribedRaid = user.Raids.Find(x => x.PokemonId == raid.PokemonId);

                if (!_db.Pokemon.ContainsKey(subscribedRaid.PokemonId.ToString())) continue;
                var pokemon = _db.Pokemon[subscribedRaid.PokemonId.ToString()];
                if (pokemon == null) continue;

                if (subscribedRaid.PokemonId != raid.PokemonId) continue;

                _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} raid is available...");

                var embed = await BuildEmbedRaid(raid, user.UserId);
                if (embed == null) continue;

                if (await CheckIfExceededNotificationLimit(user)) return;

                user.NotificationsToday++;

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(NotificationTimeout);
            }
        }

        #endregion

        #region Private Methods

        private async Task<DiscordEmbed> BuildEmbedPokemon(PokemonData pokemon, ulong userId)
        {
            var pkmn = _db.Pokemon[pokemon.PokemonId.ToString()];
            if (pkmn == null)
            {
                _logger.Error($"Failed to lookup Pokemon '{pokemon.PokemonId}' in database.");
                return null;
            }

            var user = await _client.GetMemberFromUserId(userId);
            if (user == null)
            {
                _logger.Error($"Failed to get discord member object from user id {userId}.");
                return null;
            }

            var loc = Utils.GetGoogleAddress(pokemon.Latitude, pokemon.Longitude, _config.GmapsKey);
            if (loc == null)
            {
                _logger.Error($"Failed to lookup city for coordinates {pokemon.Latitude},{pokemon.Longitude}, skipping...");
                return null;
            }

            if (!_config.CityRoles.Exists(x => string.Compare(x, loc.City, true) == 0))
            {
                File.AppendAllText("cities.txt", $"City: {loc.City}\r\n");
                return null;
            }

            if (!_client.HasRole(user, SanitizeCityName(loc.City)))
            {
                _logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role '{loc.City}'.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title        = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description  = $"{pkmn.Name}{Helpers.GetPokemonGender(pokemon.Gender)} {pokemon.CP}CP {pokemon.IV} LV{pokemon.PlayerLevel} has spawned!",
                Url          = string.Format(HttpServer.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl     = string.Format(HttpServer.GoogleMapsImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, pokemon.PokemonId),
                Color        = DiscordHelpers.BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.PokemonId}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Sta: {pokemon.Stamina}/Atk: {pokemon.Attack}/Def: {pokemon.Defense}) LV: {pokemon.PlayerLevel}");
            eb.AddField("Available Until:", $"{pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft} remaining)");
            if (loc != null)
            {
                eb.AddField("Address:", loc.Address);
            }
            eb.AddField("Location:", $"{pokemon.Latitude},{pokemon.Longitude}");
            eb.WithImageUrl(string.Format(HttpServer.GoogleMapsImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
            var embed = eb.Build();

            return embed;
        }

        private async Task<DiscordEmbed> BuildEmbedRaid(RaidData raid, ulong userId)
        {
            var pkmn = _db.Pokemon[raid.PokemonId.ToString()];
            if (pkmn == null)
            {
                _logger.Error($"Failed to lookup Raid Pokemon '{raid.PokemonId}' in database.");
                return null;
            }

            var user = await _client.GetMemberFromUserId(userId);
            if (user == null)
            {
                _logger.Error($"Failed to get discord member object from user id {userId}.");
                return null;
            }

            var loc = Utils.GetGoogleAddress(raid.Latitude, raid.Longitude, _config.GmapsKey);
            if (loc == null)
            {
                _logger.Error($"Failed to lookup city for coordinates {raid.Longitude},{raid.Longitude}, skipping...");
                return null;
            }

            if (!_config.CityRoles.Exists(x => string.Compare(x, loc.City, true) == 0))
            {
                File.AppendAllText("cities.txt", $"City: {loc.City}\r\n");
            }

            if (!_client.HasRole(user, SanitizeCityName(loc.City)))
            {
                _logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role '{loc.City}'.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title        = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description  = $"{pkmn.Name} raid is available!",
                Url          = string.Format(HttpServer.GoogleMaps, raid.Latitude, raid.Longitude),
                ImageUrl     = string.Format(HttpServer.GoogleMapsImage, raid.Latitude, raid.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, raid.PokemonId),
                Color        = DiscordHelpers.BuildRaidColor(Convert.ToInt32(raid.Level))
            };

            var fixedEndTime = DateTime.Parse(raid.EndTime.ToLongTimeString());
            var remaining = GetRaidTimeRemaining(fixedEndTime).Minutes;
            eb.AddField($"{pkmn.Name} (#{raid.PokemonId})", $"Level {raid.Level} ({Convert.ToInt32(raid.CP ?? "0").ToString("N0")} CP)");
            eb.AddField("Started:", raid.StartTime.ToLongTimeString());
            eb.AddField("Available Until:", $"{raid.EndTime.ToLongTimeString()} ({remaining} minutes left)");

            var fastMove = _db.Movesets.ContainsKey(raid.FastMove) ? _db.Movesets[raid.FastMove] : null;
            if (fastMove != null)
            {
                eb.AddField("Fast Move:", $"{fastMove.Name} ({fastMove.Type}, {fastMove.Damage} dmg, {fastMove.DamagePerSecond} dps)");
            }

            var chargeMove = _db.Movesets.ContainsKey(raid.ChargeMove) ? _db.Movesets[raid.ChargeMove] : null;
            if (chargeMove != null)
            {
                eb.AddField("Charge Move:", $"{chargeMove.Name} ({chargeMove.Type}, {chargeMove.Damage} dmg, {chargeMove.DamagePerSecond} dps)");
            }

            if (loc != null)
            {
                eb.AddField("Address:", loc.Address);
            }
            eb.AddField("Location:", $"{raid.Latitude},{raid.Longitude}");
            eb.WithImageUrl(string.Format(HttpServer.GoogleMapsImage, raid.Latitude, raid.Longitude));
            var embed = eb.Build();

            return embed;
        }

        private TimeSpan GetRaidTimeRemaining(DateTime endTime)
        {
            var start = DateTime.Now;
            var end = DateTime.Parse(endTime.ToLongTimeString());
            var remaining = TimeSpan.FromTicks(end.Ticks - start.Ticks);
            return remaining;
        }

        private void Notify(string pokemon, DiscordEmbed embed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("***********************************");
            Console.WriteLine($"********** {pokemon} FOUND **********");
            Console.WriteLine("***********************************");
            Console.WriteLine(DateTime.Now.ToString());
            Console.WriteLine("Title: \t\t{0}", embed.Title);
            Console.WriteLine(embed.Description);
            Console.WriteLine(embed.Url);
            Console.WriteLine("***********************************");
            Console.WriteLine();
            Console.ResetColor();
        }

        private string SanitizeCityName(string city)
        {
            return city
                .Replace("Rancho Cucamonga", "Upland")
                .Replace("La Verne", "Pomona")
                .Replace("Los Angeles", "EastLA")
                .Replace("East Los Angeles", "EastLA")
                .Replace("Commerce", "EastLA")
                .Replace("Santa Fe Springs", "Whittier");
        }

        private async Task<bool> CheckIfExceededNotificationLimit(Subscription<Pokemon> user)
        {
            var hasExceeded = await HasExceededNotificationLimit(user);
            if (hasExceeded)
            {
                var discordUser = await _client.GetUser(user.UserId);
                if (discordUser == null)
                {
                    _logger.Error($"Failed to find user with id {user.UserId}.");
                    return false;
                }

                if (user.Notified) return true;

                //TODO: Send direct message explaining to adjust settings.
                await _client.SendDirectMessage(discordUser, "You've exceeded the maximum amount of notifications for today, to increase this limit please consider donating in order to keep the feeds active. You may want to adjust your notification settings so you don't exceed this limit again and possibly miss an important one.", null);
                user.Notified = true;
            }

            return hasExceeded;
        }

        private async Task<bool> HasExceededNotificationLimit(Subscription<Pokemon> user)
        {
            var isModerator = user.UserId.IsModeratorOrHigher(_config);
            if (isModerator) return false;

            var isSupporter = await _client.HasSupporterRole(user.UserId, _config.SupporterRoleId);
            if (isSupporter)
            {
                if (user.NotificationsToday >= MaxNotificationsPerDaySupporter)
                {
                    return true;
                }
            }

            if (user.NotificationsToday >= MaxNotificationsPerDayNormal)
            {
                return true;
            }

            return false;
        }

        #endregion

        #region Filter Checks

        private bool MatchesIvFilter(string iv, int minimumIV)
        {
            var matchesIV = false;
            if (iv != "?")
            {
                if (!double.TryParse(iv.Replace("%", ""), out double resultIV))
                {
                    _logger.Error($"Failed to parse pokemon IV value '{iv}', skipping filter check.");
                    return false;
                }

                matchesIV |= Math.Round(resultIV) >= minimumIV;
            }

            matchesIV |= (iv == "?" && minimumIV == 0);

            return matchesIV;
        }

        private bool MatchesCpFilter(string cp, int minimumCP)
        {
            var matchesCP = false;
            if (cp != "?")
            {
                if (!int.TryParse(cp, out int resultCP))
                {
                    _logger.Error($"Failed to parse pokemon CP value '{cp}', skipping filter check.");
                    return false;
                }

                matchesCP |= resultCP >= minimumCP;
            }

            matchesCP |= (cp == "?" && minimumCP == 0);

            return matchesCP;
        }

        private bool MatchesLvlFilter(string lvl, int minimumLvl)
        {
            var matchesLvl = false;
            if (lvl != "?")
            {
                if (!int.TryParse(lvl, out int resultLvl))
                {
                    _logger.Error($"Failed to parse pokemon level value '{lvl}', skipping filter check.");
                    return false;
                }

                matchesLvl |= resultLvl >= minimumLvl;
            }

            matchesLvl |= (lvl == "?" && minimumLvl == 0);

            return matchesLvl;
        }

        #endregion
    }
}