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
    using BrockBot.Services.Geofence;
    using BrockBot.Utilities;

    public class NotificationBuilder
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;
        private readonly GeofenceService _geofenceSvc;

        #endregion

        #region Constructor

        public NotificationBuilder(DiscordClient client, Config config, IDatabase db, IEventLogger logger, GeofenceService geofenceSvc)
        {
            _client = client;
            _config = config;
            _db = db;
            _logger = logger;
            _geofenceSvc = geofenceSvc;
        }

        #endregion

        #region Public Methods

        public async Task<DiscordEmbed> BuildPokemonMessage(PokemonData pokemon, ulong userId)
        {
            var pkmn = _db.Pokemon[pokemon.Id.ToString()];
            if (pkmn == null)
            {
                _logger.Error($"Failed to lookup Pokemon '{pokemon.Id}' in database.");
                return null;
            }

            var user = await _client.GetMemberFromUserId(userId);
            if (user == null)
            {
                _logger.Error($"Failed to get discord member object from user id {userId}.");
                return null;
            }

            //var loc = Utils.GetGoogleAddress(pokemon.Latitude, pokemon.Longitude, _config.GmapsKey);
            var loc = _geofenceSvc.GetGeofence(new Location(pokemon.Latitude, pokemon.Longitude));
            if (loc == null)
            {
                _logger.Error($"Failed to lookup city from coordinates {pokemon.Latitude},{pokemon.Longitude} {pkmn.Name} {pokemon.IV}, skipping...");
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
                _logger.Debug($"Skipping user {user.DisplayName} ({user.Id}) for {pkmn.Name} {pokemon.IV}, no city role '{loc.Name}'.");
                return null;
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.Name) ? "DIRECTIONS" : loc.Name,
                Description = $"{pkmn.Name}{pokemon.Gender.GetPokemonGenderIcon()} {pokemon.CP}CP {pokemon.IV} LV{pokemon.Level} has spawned!",
                Url = string.Format(Strings.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.Id),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.Id}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Atk: {pokemon.Attack}/Def: {pokemon.Defense}/Sta: {pokemon.Stamina}) LV: {pokemon.Level}");
            if (!string.IsNullOrEmpty(pokemon.FormId))
            {
                var form = pokemon.Id.GetPokemonForm(pokemon.FormId);
                if (!string.IsNullOrEmpty(form))
                {
                    eb.AddField("Form:", form);
                }
            }

            if (pokemon.Level != "?")
            {
                if (int.TryParse(pokemon.Level, out int lvl))
                {
                    var maxCp = _db.MaxCpAtLevel(pokemon.Id, lvl);
                    eb.AddField("Max CP @ Level 40:", maxCp.ToString("N0"));
                }
            }

            if (pkmn.Types.Count > 0)
            {
                var types = new List<string>();
                pkmn.Types.ForEach(x =>
                {
                    if (Strings.TypeEmojis.ContainsKey(x.Type.ToLower()))
                    {
                        types.Add(Strings.TypeEmojis[x.Type.ToLower()]);
                    }
                });
                eb.AddField("Types: ", string.Join("/", types));
            }

            if (pokemon.Height != "?" && pokemon.Weight != "?")
            {
                if (float.TryParse(pokemon.Height, out float height) && float.TryParse(pokemon.Weight, out float weight))
                {
                    var size = _db.GetSize(pokemon.Id, height, weight);
                    eb.AddField("Size:", size);
                }
            }

            var fastMove = _db.Movesets.ContainsKey(pokemon.FastMove) ? _db.Movesets[pokemon.FastMove] : null;
            if (fastMove != null)
            {
                var fastMoveIcon = Strings.TypeEmojis.ContainsKey(fastMove.Type.ToLower()) ? Strings.TypeEmojis[fastMove.Type.ToLower()] : fastMove.Type;
                eb.AddField("Fast Move:", $"{fastMove.Name} ({fastMoveIcon}, {fastMove.Damage} dmg, {fastMove.DamagePerSecond} dps)");
            }

            var chargeMove = _db.Movesets.ContainsKey(pokemon.ChargeMove) ? _db.Movesets[pokemon.ChargeMove] : null;
            if (chargeMove != null)
            {
                var chargeMoveIcon = Strings.TypeEmojis.ContainsKey(chargeMove.Type.ToLower()) ? Strings.TypeEmojis[chargeMove.Type.ToLower()] : chargeMove.Type;
                eb.AddField("Charge Move:", $"{chargeMove.Name} ({chargeMoveIcon}, {chargeMove.Damage} dmg, {chargeMove.DamagePerSecond} dps)");
            }

            eb.AddField("Despawn:", $"{pokemon.DespawnTime.ToLongTimeString()} ({Utils.ToReadableString(pokemon.SecondsLeft, true)} left)");
            eb.AddField("Location:", $"{Math.Round(pokemon.Latitude, 5)},{Math.Round(pokemon.Longitude, 5)}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
            var embed = eb.Build();

            return embed;
        }

        public async Task<DiscordEmbed> BuildRaidMessage(RaidData raid, ulong userId)
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
            var loc = _geofenceSvc.GetGeofence(new Location(raid.Latitude, raid.Longitude));
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
            var remaining = GetRaidTimeRemaining(fixedEndTime);
            eb.AddField($"{pkmn.Name} (#{raid.PokemonId})", $"Level {raid.Level} ({Convert.ToInt32(raid.CP ?? "0").ToString("N0")} CP)");
            eb.AddField("Started:", raid.StartTime.ToLongTimeString());
            eb.AddField("Despawn:", $"{raid.EndTime.ToLongTimeString()} ({Utils.ToReadableString(remaining, true)} left)");

            if (pkmn.Types.Count > 0)
            {
                var types = new List<string>();
                pkmn.Types.ForEach(x =>
                {
                    types.Add(Strings.TypeEmojis[x.Type.ToLower()]);
                });
                eb.AddField("Types: ", string.Join("/", types));
            }

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

            eb.AddField("Location:", $"{Math.Round(raid.Latitude, 5)},{Math.Round(raid.Longitude, 5)}");
            eb.WithImageUrl(string.Format(Strings.GoogleMapsStaticImage, raid.Latitude, raid.Longitude));
            var embed = eb.Build();

            return embed;
        }

        #endregion

        #region Private Methods

        private TimeSpan GetRaidTimeRemaining(DateTime endTime)
        {
            var start = DateTime.Now;
            var end = DateTime.Parse(endTime.ToLongTimeString());
            var remaining = TimeSpan.FromTicks(end.Ticks - start.Ticks);
            return remaining;
        }

        #endregion
    }
}