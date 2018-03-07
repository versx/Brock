namespace BrockBot.Services.Notifications
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

        private readonly Filters _filters;
        private readonly NotificationBuilder _builder;

        private Dictionary<string, ulong> _uniqueMessageIds;

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

            _filters = new Filters(_logger);
            _uniqueMessageIds = new Dictionary<string, ulong>();

            GeofenceSvc = new GeofenceService(GeofenceItem.Load(_config.GeofenceFolder, _config.CityRoles));
            _builder = new NotificationBuilder(_client, _config, _db, _logger, GeofenceSvc);
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

            if (!_db.Pokemon.ContainsKey(pkmn.Id.ToString())) return;
            var pokemon = _db.Pokemon[pkmn.Id.ToString()];
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

                    if (!_filters.AlarmMatchesIV(pkmn.IV, alarm.Filters))
                        continue;

                    var embed = BuildEmbedPokemonFromAlarm(pkmn, alarm);
                    if (embed == null) continue;

                    _logger.Info($"Notifying alarm {alarm.Name} that a {pokemon.Name} {pkmn.CP}CP {pkmn.IV} IV L{pkmn.Level} has spawned...");
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

            //var onlyEventPokemon = true;
            //var eventPokemon = new List<int> { 0 };
            //var isEventPokemon = eventPokemon.Contains(pkmn.PokemonId);
            //if (onlyEventPokemon && !isEventPokemon)
            //{
            //    //TODO: Only send event Pokemon subscriptions of 90% IV and higher.
            //    return;
            //}

            //TODO: Split up subscriptions list to multiple threads to check or add some kind of queue.

            DiscordUser discordUser;
            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                try
                {
                    var user = _db.Subscriptions[i];
                    if (user == null) continue;
                    if (!user.Enabled) continue;

                    discordUser = await _client.GetUser(user.UserId);
                    if (discordUser == null)
                    {
                        if (!_db.Subscriptions.Remove(user))
                        {
                            _logger.Error($"Failed to remove non-existing user {user.UserId} from database.");
                        }
                        continue;
                    }

                    if (!user.Pokemon.Exists(x => x.PokemonId == pkmn.Id)) continue;
                    var subscribedPokemon = user.Pokemon.Find(x => x.PokemonId == pkmn.Id);
                    if (subscribedPokemon == null) continue;

                    if (!_db.Pokemon.ContainsKey(subscribedPokemon.PokemonId.ToString())) continue;
                    var pokemon = _db.Pokemon[subscribedPokemon.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    if (_client == null) continue;
                    //if (!await _client.IsSupporterOrHigher(user.UserId, _config)) continue;

                    var matchesIV = _filters.MatchesIV(pkmn.IV, subscribedPokemon.MinimumIV);
                    //var matchesCP = MatchesCpFilter(pkmn.CP, subscribedPokemon.MinimumCP);
                    var matchesLvl = _filters.MatchesLvl(pkmn.Level, subscribedPokemon.MinimumLevel);
                    var matchesGender = _filters.MatchesGender(pkmn.Gender, subscribedPokemon.Gender);

                    if (!(matchesIV && matchesLvl && matchesGender)) continue;

                    if (user.NotificationLimiter.IsLimited())
                    {
                        if (!user.NotifiedOfLimited)
                        {
                            await _client.SendDirectMessage(discordUser, string.Format(NotificationsLimitedMessage, NotificationLimiter.MaxNotificationsPerMinute), null);
                            user.NotifiedOfLimited = true;
                        }

                        continue;
                    }

                    user.NotifiedOfLimited = false;

                    _logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} {pkmn.CP}CP {pkmn.IV} IV L{pkmn.Level} has spawned...");

                    var embed = await _builder.BuildPokemonMessage(pkmn, user.UserId);
                    if (embed == null) continue;

                    //if (await CheckIfExceededNotificationLimit(user)) return;

                    user.NotificationsToday++;

                    await SendNotification(discordUser, pokemon.Name, embed);
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
                    if (discordUser == null)
                    {
                        if (!_db.Subscriptions.Remove(user))
                        {
                            _logger.Error($"Failed to remove non-existing user {user.UserId} from database.");
                        }
                        continue;
                    }

                    if (!user.Raids.Exists(x => x.PokemonId == raid.PokemonId)) continue;
                    var subscribedRaid = user.Raids.Find(x => x.PokemonId == raid.PokemonId);
                    if (subscribedRaid == null) continue;

                    //if (!_db.Pokemon.ContainsKey(subscribedRaid.PokemonId.ToString())) continue;
                    //var pokemon = _db.Pokemon[subscribedRaid.PokemonId.ToString()];
                    //if (pokemon == null) continue;

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

                        continue;
                    }

                    user.NotifiedOfLimited = false;

                    _logger.Info($"Notifying user {discordUser.Username} that a {raid.PokemonId} raid is available...");

                    var embed = await _builder.BuildRaidMessage(raid, user.UserId);
                    if (embed == null) continue;

                    //if (await CheckIfExceededNotificationLimit(user)) return;

                    user.NotificationsToday++;

                    await SendNotification(discordUser, raid.PokemonId.ToString(), embed);
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

        private async Task SendNotification(DiscordUser user, string pokemon, DiscordEmbed embed)
        {
            Notify(pokemon, embed);

            await _client.SendDirectMessage(user, string.Empty, embed);
            await Utils.Wait(NotificationTimeout);
        }

        #region Private Methods

        private DiscordEmbed BuildEmbedPokemonFromAlarm(PokemonData pokemon, AlarmObject alarm)
        {
            var pkmn = _db.Pokemon[pokemon.Id.ToString()];
            if (pkmn == null)
            {
                _logger.Error($"Failed to lookup Pokemon '{pokemon.Id}' in database.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = alarm == null || string.IsNullOrEmpty(alarm.Name) ? "DIRECTIONS" : alarm.Name,
                Description = $"{pkmn.Name}{pokemon.Gender.GetPokemonGenderIcon()} {pokemon.CP}CP {pokemon.IV} LV{pokemon.Level} has spawned!",
                Url = string.Format(Strings.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.Id),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.Id}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Sta: {pokemon.Stamina}/Atk: {pokemon.Attack}/Def: {pokemon.Defense}) LV: {pokemon.Level}");
            if (!string.IsNullOrEmpty(pokemon.FormId))
            {
                var form = pokemon.Id.GetPokemonForm(pokemon.FormId);
                if (!string.IsNullOrEmpty(form))
                {
                    eb.AddField("Form:", form);
                }
            }

            if (pkmn.Types.Count > 0)
            {
                var types = new List<string>();
                pkmn.Types.ForEach(x =>
                {
                    types.Add(Strings.TypeEmojis[x.Type.ToLower()]);
                });
                eb.AddField("Types: ", string.Join("/", types));
            }

            eb.AddField("Despawn:", $"{pokemon.DespawnTime.ToLongTimeString()} ({Utils.ToReadableString(pokemon.SecondsLeft, true)} left)");
            eb.AddField("Location:", $"{Math.Round(pokemon.Latitude, 5)},{Math.Round(pokemon.Longitude, 5)}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
            var embed = eb.Build();

            return embed;
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

        private bool MatchesGeofenceFilter(GeofenceItem geofence, Location location)
        {
            return GeofenceSvc.Contains(geofence, location);
        }

        #endregion
    }
}