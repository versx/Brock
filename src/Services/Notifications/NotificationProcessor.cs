﻿namespace BrockBot.Services.Notifications
{
    using System;
    using System.Collections.Generic;
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

            var isEventPokemon = _config.EventPokemon.Contains(Convert.ToUInt32(pkmn.Id));
            if (_config.OnlySendEventPokemon && !isEventPokemon)
            {
                _logger.Info($"Only event Pokemon can be sent with a minimum IV of {_config.EventPokemonMinimumIV}%.");
                return;
            }

            //TODO: Split up subscriptions list to multiple threads to check or add some kind of queue.

            DiscordUser discordUser;
            Subscription<Pokemon> user;
            //bool isSupporter;
            Pokemon subscribedPokemon;
            PokemonInfo pokemon;
            bool matchesIV;
            bool matchesLvl;
            bool matchesGender;
            DiscordEmbed embed;

            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                try
                {
                    user = _db.Subscriptions[i];
                    if (user == null) continue;
                    if (!user.Enabled) continue;

                    //isSupporter = await _client.IsSupporterOrHigher(user.UserId, _config);
                    //if (pkmn.Id == 132 && !isSupporter)
                    //{
                    //    _logger.Debug($"User {user.UserId} is not a supporter, Ditto has been skipped...");
                    //    continue;
                    //}

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
                    subscribedPokemon = user.Pokemon.Find(x => x.PokemonId == pkmn.Id);
                    if (subscribedPokemon == null) continue;

                    if (!_db.Pokemon.ContainsKey(subscribedPokemon.PokemonId.ToString())) continue;
                    pokemon = _db.Pokemon[subscribedPokemon.PokemonId.ToString()];
                    if (pokemon == null) continue;

                    if (_client == null) continue;
                    //if (!await _client.IsSupporterOrHigher(user.UserId, _config)) continue;

                    matchesIV = _filters.MatchesIV(pkmn.IV, _config.OnlySendEventPokemon ? _config.EventPokemonMinimumIV : subscribedPokemon.MinimumIV);
                    //var matchesCP = MatchesCpFilter(pkmn.CP, subscribedPokemon.MinimumCP);
                    matchesLvl = _filters.MatchesLvl(pkmn.Level, subscribedPokemon.MinimumLevel);
                    matchesGender = _filters.MatchesGender(pkmn.Gender, subscribedPokemon.Gender);

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

                    embed = await _builder.BuildPokemonMessage(pkmn, user.UserId);
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
            Subscription<Pokemon> user;
            Pokemon subscribedRaid;
            DiscordEmbed embed;

            for (int i = 0; i < _db.Subscriptions.Count; i++)
            {
                try
                {
                    user = _db.Subscriptions[i];
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
                    subscribedRaid = user.Raids.Find(x => x.PokemonId == raid.PokemonId);
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

                    embed = await _builder.BuildRaidMessage(raid, user.UserId);
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
            var total = 0ul;
            var pokemon = 0;
            var raids = 0;
            foreach (var sub in _db.Subscriptions)
            {
                var user = await _client.GetUser(sub.UserId);
                if (user == null)
                {
                    _logger.Error($"Failed to get discord user from id {sub.UserId}.");
                }

                msg += $"{user.Username} ({sub.UserId}): Pokemon Subscriptions: {sub.Pokemon.Count}, Raid Subscriptions: {sub.Raids.Count}, Total Notifications: {sub.NotificationsToday}\r\n";
                total += sub.NotificationsToday;
                pokemon += sub.Pokemon.Count;
                raids += sub.Raids.Count;
            }

            msg = $"Total Notifications: {total}, Total Pokemon Subscriptions: {pokemon}, Total Raid Subscriptions: {raids}\r\n{msg}";

            var now = DateTime.Now;
            var path = Path.Combine(statsDirectory, $"notifications-{now.ToString("yyyy-MM-dd_hhmmss")}.txt");
            File.WriteAllText(path, msg);
        }

        #endregion

        #region Private Methods

        private async Task SendNotification(DiscordUser user, string pokemon, DiscordEmbed embed)
        {
            Notify(pokemon, embed);

            await _client.SendDirectMessage(user, string.Empty, embed);
            await Utils.Wait(NotificationTimeout);
        }

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
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.Id, 0),
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

            eb.AddField("Despawn:", $"{pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft.ToReadableString(true)} left)");
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

        #endregion

        #region Filter Checks

        private bool MatchesGeofenceFilter(GeofenceItem geofence, Location location)
        {
            return GeofenceSvc.Contains(geofence, location);
        }

        #endregion
    }
}