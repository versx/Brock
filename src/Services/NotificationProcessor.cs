namespace BrockBot.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Utilities;

    public class NotificationProcessor
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public NotificationProcessor(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;
        }

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

                //if (ignoreMissingCPIV && (pkmn.CP == "?" || pkmn.IV == "?") && subscribedPokemon.MinimumIV > 0) continue;

                var matchesIV = false;
                if (pkmn.IV != "?")
                {
                    if (!int.TryParse(pkmn.IV.Replace("%", ""), out int resultIV))
                    {
                        _logger.Error($"Failed to parse pokemon IV value '{pkmn.IV}', skipping filter check.");
                        continue;
                    }

                    matchesIV |= resultIV >= subscribedPokemon.MinimumIV;
                }

                //var matchesCP = false;
                //if (pkmn.CP != "?")
                //{
                //    if (!int.TryParse(pkmn.CP, out int resultCP))
                //    {
                //        Logger.Error($"Failed to parse pokemon CP {pkmn.CP}, skipping filter check.");
                //        continue;
                //    }

                //    matchesCP |= resultCP >= subscribedPokemon.MinimumCP;
                //}

                //var matchesLvl = false;
                //if (pkmn.PlayerLevel != "?")
                //{
                //    if (!int.TryParse(pkmn.PlayerLevel, out int resultLvl))
                //    {
                //        _logger.Error($"Failed to parse pokemon level value '{pkmn.PlayerLevel}', skipping filter check.");
                //        continue;
                //    }

                //    matchesLvl |= resultLvl >= subscribedPokemon.MinimumLevel;
                //}

                if (pkmn.IV == "?" && subscribedPokemon.MinimumIV == 0)
                {
                    matchesIV = true;
                }

                if (!matchesIV) continue;
                //if (!(matchesIV || matchesCP || matchesLvl)) continue;

                _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} CP{pkmn.CP} {pkmn.IV} IV L{pkmn.PlayerLevel} has spawned...");

                var embed = await BuildEmbedPokemon(pkmn, user.UserId);
                if (embed == null) continue;

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(10);
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

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(10);
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
                Title = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description = $"{pkmn.Name}{Helpers.GetPokemonGender(pokemon.Gender)} {pokemon.CP}CP {pokemon.IV} LV{pokemon.PlayerLevel} has spawned!",
                Url = string.Format(HttpServer.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(HttpServer.GoogleMapsImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, pokemon.PokemonId),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
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
                Title = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description = $"{pkmn.Name} raid is available!",
                Url = string.Format(HttpServer.GoogleMaps, raid.Latitude, raid.Longitude),
                ImageUrl = string.Format(HttpServer.GoogleMapsImage, raid.Latitude, raid.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, raid.PokemonId),
                Color = DiscordHelpers.BuildRaidColor(Convert.ToInt32(raid.Level))
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
            //Console.WriteLine("Title: \t\t{0}", embed.Title); //DIRECTIONS
            Console.WriteLine(embed.Description); //CP, IV, etc...
            Console.WriteLine(embed.Url); //GMaps link
            Console.WriteLine("***********************************");
            Console.WriteLine();
            Console.ResetColor();
        }

        private string SanitizeCityName(string city)
        {
            return city
                .Replace("Rancho Cucamonga", "Upland")
                .Replace("La Verne", "Pomona")
                .Replace("Santa Fe Springs", "Whittier");
        }

        #endregion
    }
}