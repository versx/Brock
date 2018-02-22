namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Services.Alarms;
    using BrockBot.Services.Geofence;
    using BrockBot.Utilities;

    public class NotificationProcessor
    {
        #region Constants

        public const int NotificationTimeout = 10;
        public const string StatsFolder = "Stats";
        public const string NotificationsLimitedMessage = "Your Pokemon notifications have exceeded {0} per minute so you've been limited for the next 60 seconds, please consider adjusting your settings to prevent this.";

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public GeofenceService GeofenceSvc { get; }

        #endregion

        #region Constructor

        public NotificationProcessor(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _config = config;
            _logger = logger;

            GeofenceSvc = new GeofenceService(GeofenceItem.Load(_config.GeofenceFolder, _config.CityRoles));
        }

        #endregion

        #region Public Methods

        public async Task ProcessAlarms(PokemonData pkmn, Dictionary<string, List<AlarmObject>> alarms)
        {
            if (pkmn == null) return;
            if (alarms == null || alarms.Count == 0) return;

            if (_db == null)
            {
                _logger.Error($"Database is not initialized...");
                return;
            }

            if (!_db.Pokemon.ContainsKey(pkmn.PokemonId.ToString())) return;
            var pokemon = _db.Pokemon[pkmn.PokemonId.ToString()];
            if (pokemon == null) return;

            if (_client == null) return;

            foreach (var item in alarms)
            {
                foreach (var alarm in item.Value)
                {
                    //if (string.Compare(alarm.Geofence.Name, item.Key, true) != 0)
                    //    continue;

                    if (!MatchesGeofenceFilter(alarm.Geofence, new Location(pkmn.Latitude, pkmn.Longitude)))
                        continue;

                    if (!AlarmMatchesIvFilters(pkmn.IV, alarm.Filters))
                        continue;

                    var embed = BuildEmbedPokemonFromAlarm(pkmn, alarm);
                    if (embed == null) continue;

                    _logger.Info($"Notifying alarm {alarm.Name} that a {pokemon.Name} {pkmn.CP}CP {pkmn.IV} IV L{pkmn.PlayerLevel} has spawned...");
                    Notify(pokemon.Name, embed);

                    await _client.SendMessage(alarm.Webhook, null, embed);
                }
            }
        }

        public async Task ProcessPokemon(PokemonData pkmn)
        {
            if (pkmn == null) return;

            if (_db == null)
            {
                _logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                try
                {
                    var user = _db.Subscriptions[i];
                    if (user == null) continue;
                    if (!user.Enabled) continue;

                    discordUser = await _client.GetUser(user.UserId);
                    if (discordUser == null) continue;

                    if (!user.Pokemon.Exists(x => x.PokemonId == pkmn.PokemonId)) continue;
                    var subscribedPokemon = user.Pokemon.Find(x => x.PokemonId == pkmn.PokemonId);
                    if (subscribedPokemon == null) continue;

                    if (!_db.Pokemon.ContainsKey(subscribedPokemon.PokemonId.ToString())) continue;
                    var pokemon = _db.Pokemon[subscribedPokemon.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    if (_client == null) continue;
                    //if (!await _client.IsSupporterOrHigher(user.UserId, _config)) continue;

                    var matchesIV = MatchesIvFilter(pkmn.IV, subscribedPokemon.MinimumIV);
                    //var matchesCP = MatchesCpFilter(pkmn.CP, subscribedPokemon.MinimumCP);
                    var matchesLvl = MatchesLvlFilter(pkmn.PlayerLevel, subscribedPokemon.MinimumLevel);
                    //var matchesGender = MatchesGenderFilter(pkmn.Gender, subscribedPokemon.MinimumGender);
                    //var matchesAtk = MatchesAttackFilter(pkmn.Attack, subscribedPokemon.MinimumAtk);
                    //var matchesDef = MatchesDefenseFilter(pkmn.Defense, subscribedPokemon.MinimumDef);
                    //var matchesSta = MatchesStaminaFilter(pkmn.Stamina, subscribedPokemon.MinimumSta);
                    //var matchesGeofence = MatchesGeofenceFilter(null, new Location(pkmn.Latitude, pkmn.Longitude));

                    //if (!(matchesIV || matchesCP || matchesLvl || matchesGender || matchesAtk || matchesDef || matchesSta)) continue;
                    if (!(matchesIV && matchesLvl)) continue;

                    if (user.NotificationLimiter.IsLimited())
                    {
                        if (!user.NotifiedOfLimited)
                        {
                            await _client.SendDirectMessage(discordUser, string.Format(NotificationsLimitedMessage, NotificationLimiter.MaxNotificationsPerMinute), null);
                            user.NotifiedOfLimited = true;
                        }

                        return;
                    }

                    user.NotifiedOfLimited = false;

                    _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} {pkmn.CP}CP {pkmn.IV} IV L{pkmn.PlayerLevel} has spawned...");

                    var embed = await BuildEmbedPokemon(pkmn, user.UserId);
                    if (embed == null) continue;

                    //if (await CheckIfExceededNotificationLimit(user)) return;

                    user.NotificationsToday++;

                    Notify(pokemon.Name, embed);

                    await _client.SendDirectMessage(discordUser, string.Empty, embed);
                    await Utils.Wait(NotificationTimeout);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }

        public async Task ProcessRaid(RaidData raid)
        {
            if (raid == null) return;

            if (_db == null)
            {
                _logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                try
                {
                    var user = _db.Subscriptions[i];
                    if (user == null) continue;
                    if (!user.Enabled) continue;

                    discordUser = await _client.GetUser(user.UserId);
                    if (discordUser == null) continue;

                    if (!user.Raids.Exists(x => x.PokemonId == raid.PokemonId)) continue;
                    var subscribedRaid = user.Raids.Find(x => x.PokemonId == raid.PokemonId);
                    if (subscribedRaid == null) continue;

                    if (!_db.Pokemon.ContainsKey(subscribedRaid.PokemonId.ToString())) continue;
                    var pokemon = _db.Pokemon[subscribedRaid.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    if (_client == null) continue;
                    //if (!await _client.IsSupporterOrHigher(user.UserId, _config)) continue;

                    if (subscribedRaid.PokemonId != raid.PokemonId) continue;

                    if (user.NotificationLimiter.IsLimited())
                    {
                        if (!user.NotifiedOfLimited)
                        {
                            await _client.SendDirectMessage(discordUser, string.Format(NotificationsLimitedMessage, NotificationLimiter.MaxNotificationsPerMinute), null);
                            user.NotifiedOfLimited = true;
                        }

                        return;
                    }

                    user.NotifiedOfLimited = false;

                    _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} raid is available...");

                    var embed = await BuildEmbedRaid(raid, user.UserId);
                    if (embed == null) continue;

                    //if (await CheckIfExceededNotificationLimit(user)) return;

                    user.NotificationsToday++;

                    Notify(pokemon.Name, embed);

                    await _client.SendDirectMessage(discordUser, string.Empty, embed);
                    await Utils.Wait(NotificationTimeout);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }

        public async Task WriteStatistics()
        {
            var statsDirectory = Path.Combine(Directory.GetCurrentDirectory(), StatsFolder);
            if (!Directory.Exists(statsDirectory))
            {
                Directory.CreateDirectory(statsDirectory);
            }

            var msg = string.Empty;
            var subs = _db.Subscriptions;
            foreach (var sub in _db.Subscriptions)
            {
                var user = await _client.GetUser(sub.UserId);
                if (user == null)
                {
                    _logger.Error($"Failed to get discord user from id {sub.UserId}.");
                }

                msg += $"{user.Username} ({sub.UserId}): Pokemon Subscriptions: {sub.Pokemon.Count}, Raid Subscriptions: {sub.Raids.Count}, Total Notifications: {sub.NotificationsToday}\r\n";
            }
            var now = DateTime.Now;
            var path = Path.Combine(statsDirectory, $"notifications-{now.ToString("yyyy-MM-dd_hhmmss")}.txt");
            File.WriteAllText(path, msg);
        }

        #endregion

        #region Private Methods

        private DiscordEmbed BuildEmbedPokemonFromAlarm(PokemonData pokemon, AlarmObject alarm)
        {
            var pkmn = _db.Pokemon[pokemon.PokemonId.ToString()];
            if (pkmn == null)
            {
                _logger.Error($"Failed to lookup Pokemon '{pokemon.PokemonId}' in database.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = alarm == null || string.IsNullOrEmpty(alarm.Name) ? "DIRECTIONS" : alarm.Name,
                Description = $"{pkmn.Name}{Helpers.GetPokemonGender(pokemon.Gender)} {pokemon.CP}CP {pokemon.IV} LV{pokemon.PlayerLevel} has spawned!",
                Url = string.Format(Strings.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.PokemonId),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.PokemonId}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Sta: {pokemon.Stamina}/Atk: {pokemon.Attack}/Def: {pokemon.Defense}) LV: {pokemon.PlayerLevel}");
            eb.AddField("Available Until:", $"{pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft} remaining)");
            eb.AddField("Location:", $"{pokemon.Latitude},{pokemon.Longitude}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
            var embed = eb.Build();

            return embed;
        }

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

            //var loc = Utils.GetGoogleAddress(pokemon.Latitude, pokemon.Longitude, _config.GmapsKey);
            var loc = GeofenceSvc.GetGeofence(new Location(pokemon.Latitude, pokemon.Longitude));
            if (loc == null)
            {
                _logger.Error($"Failed to lookup city for coordinates {pokemon.Latitude},{pokemon.Longitude}, skipping...");
                return null;
            }

            if (!_config.CityRoles.Exists(x => string.Compare(x, loc.Name, true) == 0))
            {
                File.AppendAllText("cities.txt", $"City: {loc.Name}\r\n");
                return null;
            }

            //if (!_client.HasRole(user, SanitizeCityName(loc.Name)))
            if (!_client.HasRole(user, loc.Name))
            {
                _logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role '{loc.Name}'.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.Name) ? "DIRECTIONS" : loc.Name,
                Description = $"{pkmn.Name}{Helpers.GetPokemonGender(pokemon.Gender)} {pokemon.CP}CP {pokemon.IV} LV{pokemon.PlayerLevel} has spawned!",
                Url = string.Format(Strings.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.PokemonId),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.PokemonId}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Atk: {pokemon.Attack}/Def: {pokemon.Defense}/Sta: {pokemon.Stamina}) LV: {pokemon.PlayerLevel}");
            eb.AddField("Available Until:", $"{pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft} remaining)");
            eb.AddField("Location:", $"{pokemon.Latitude},{pokemon.Longitude}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
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

            //var loc = Utils.GetGoogleAddress(raid.Latitude, raid.Longitude, _config.GmapsKey);
            var loc = GeofenceSvc.GetGeofence(new Location(raid.Latitude, raid.Longitude));
            if (loc == null)
            {
                _logger.Error($"Failed to lookup city for coordinates {raid.Longitude},{raid.Longitude}, skipping...");
                return null;
            }

            if (!_config.CityRoles.Exists(x => string.Compare(x, loc.Name, true) == 0))
            {
                File.AppendAllText("cities.txt", $"City: {loc.Name}\r\n");
            }

            if (!_client.HasRole(user, loc.Name))
            {
                _logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role '{loc.Name}'.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.Name) ? "DIRECTIONS" : loc.Name,
                Description = $"{pkmn.Name} raid is available!",
                Url = string.Format(Strings.GoogleMaps, raid.Latitude, raid.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, raid.Latitude, raid.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, raid.PokemonId),
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

            eb.AddField("Location:", $"{raid.Latitude},{raid.Longitude}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, raid.Latitude, raid.Longitude));
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

        //private string SanitizeCityName(string city)
        //{
        //    return city
        //        .Replace("Rancho Cucamonga", "Upland")
        //        .Replace("La Verne", "Pomona")
        //        .Replace("Diamond Bar", "Pomona")
        //        .Replace("Los Angeles", "EastLA")
        //        .Replace("East Los Angeles", "EastLA")
        //        .Replace("Commerce", "EastLA")
        //        .Replace("Santa Fe Springs", "Whittier");
        //}

        //private async Task<bool> CheckIfExceededNotificationLimit(Subscription<Pokemon> user)
        //{
        //    var hasExceeded = await HasExceededNotificationLimit(user);
        //    if (hasExceeded)
        //    {
        //        var discordUser = await _client.GetUser(user.UserId);
        //        if (discordUser == null)
        //        {
        //            _logger.Error($"Failed to find user with id {user.UserId}.");
        //            return false;
        //        }

        //        if (user.Notified) return true;

        //        //TODO: Send direct message explaining to adjust settings.
        //        await _client.SendDirectMessage(discordUser, "You've exceeded the maximum amount of notifications for today, to increase this limit please consider donating in order to keep the feeds active. You may want to adjust your notification settings so you don't exceed this limit again and possibly miss an important one.", null);
        //        user.Notified = true;
        //    }

        //    return hasExceeded;
        //}

        //private async Task<bool> HasExceededNotificationLimit(Subscription<Pokemon> user)
        //{
        //    var isModerator = user.UserId.IsModeratorOrHigher(_config);
        //    if (isModerator) return false;

        //    var isSupporter = await _client.HasSupporterRole(user.UserId, _config.SupporterRoleId);
        //    if (isSupporter)
        //    {
        //        if (user.NotificationsToday >= MaxNotificationsPerDaySupporter)
        //        {
        //            return true;
        //        }
        //    }

        //    return user.NotificationsToday >= MaxNotificationsPerDayNormal;
        //}

        #endregion

        #region Filter Checks

        private bool AlarmMatchesIvFilters(string iv, List<FilterObject> filters, bool ignoreMissing = true)
        {
            var matchesIV = false;
            if (iv != "?")
            {
                if (!double.TryParse(iv.Replace("%", ""), out double resultIV))
                {
                    _logger.Error($"Failed to parse pokemon IV value '{iv}', skipping filter check.");
                    return false;
                }

                foreach (var filter in filters)
                {
                    matchesIV |= Math.Round(resultIV) >= filter.MinimumIV && Math.Round(resultIV) <= filter.MaximumIV;
                }
            }

            matchesIV |= (iv == "?" && !ignoreMissing);

            return matchesIV;
        }

        private bool MatchesIvFilter(string iv, int minimumIV, int maximumIV)
        {
            var matchesIV = false;
            if (iv != "?")
            {
                if (!double.TryParse(iv.Replace("%", ""), out double resultIV))
                {
                    _logger.Error($"Failed to parse pokemon IV value '{iv}', skipping filter check.");
                    return false;
                }

                matchesIV |= Math.Round(resultIV) >= minimumIV && Math.Round(resultIV) <= maximumIV;
            }

            matchesIV |= (iv == "?" && minimumIV == 0);

            return matchesIV;
        }

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

        private bool MatchesGeofenceFilter(GeofenceItem geofence, Location location)
        {
            return GeofenceSvc.Contains(geofence, location);
        }

        private bool MatchesGenderFilter(PokemonGender gender, PokemonGender desiredGender)
        {
            return gender == desiredGender ||
                   (gender == PokemonGender.Unset ||
                   gender == PokemonGender.Genderless);
        }

        private bool MatchesAttackFilter(string atk, int minimumAtk)
        {
            var matchesAtk = false;
            if (atk != "?")
            {
                if (!int.TryParse(atk, out int resultAtk))
                {
                    _logger.Error($"Failed to parse pokemon attack IV value '{atk}', skipping filter check.");
                    return false;
                }

                matchesAtk |= resultAtk >= minimumAtk;
            }

            matchesAtk |= (atk == "?" && minimumAtk == 0);

            return matchesAtk;
        }

        private bool MatchesDefenseFilter(string def, int minimumDef)
        {
            var matchesDef = false;
            if (def != "?")
            {
                if (!int.TryParse(def, out int resultAtk))
                {
                    _logger.Error($"Failed to parse pokemon defense IV value '{def}', skipping filter check.");
                    return false;
                }

                matchesDef |= resultAtk >= minimumDef;
            }

            matchesDef |= (def == "?" && minimumDef == 0);

            return matchesDef;
        }

        private bool MatchesStaminaFilter(string sta, int minimumSta)
        {
            var matchesSta = false;
            if (sta != "?")
            {
                if (!int.TryParse(sta, out int resultAtk))
                {
                    _logger.Error($"Failed to parse pokemon stamina IV value '{sta}', skipping filter check.");
                    return false;
                }

                matchesSta |= resultAtk >= minimumSta;
            }

            matchesSta |= (sta == "?" && minimumSta == 0);

            return matchesSta;
        }

        #endregion
    }
}