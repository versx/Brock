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

            var form = pokemon.Id.GetPokemonForm(pokemon.FormId);
            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.Name) ? "DIRECTIONS" : loc.Name,
                //Description = $"{pkmn.Name}{pokemon.Gender.GetPokemonGenderIcon()} {pokemon.CP}CP {pokemon.IV} Despawn: {pokemon.DespawnTime.ToLongTimeString()}",
                Url = string.Format(Strings.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokemon.Id, Convert.ToInt32(pokemon.FormId ?? "0")),
                Color = DiscordHelpers.BuildColor(pokemon.IV)
            };

            if (pokemon.IV == "?")
            {
                eb.Description = $"{pkmn.Name} {form}{pokemon.Gender.GetPokemonGenderIcon()} Despawn: {pokemon.DespawnTime.ToLongTimeString()}\r\n";
            }
            else
            {
                eb.Description = $"{pkmn.Name} {form}{pokemon.Gender.GetPokemonGenderIcon()} {pokemon.IV} L{pokemon.Level} Despawn: {pokemon.DespawnTime.ToLongTimeString()}\r\n\r\n";
                eb.Description += $"**Details:** CP: {pokemon.CP} IV: {pokemon.IV} LV: {pokemon.Level}\r\n";
            }
            eb.Description += $"**Despawn:** {pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft.ToReadableStringNoSeconds()} left)\r\n";
            if (pokemon.Attack != "?" && pokemon.Defense != "?" && pokemon.Stamina != "?")
            {
                eb.Description += $"**IV Stats:** Atk: {pokemon.Attack}/Def: {pokemon.Defense}/Sta: {pokemon.Stamina}\r\n";
            }

            if (!string.IsNullOrEmpty(form))
            {
                eb.Description += $"**Form:** {form}\r\n";
            }

            if (int.TryParse(pokemon.Level, out int lvl) && lvl >= 30)
            {
                eb.Description += $":white_sun_rain_cloud: Boosted\r\n";
            }

            var maxCp = _db.MaxCpAtLevel(pokemon.Id, 40);
            var maxWildCp = _db.MaxCpAtLevel(pokemon.Id, 35);
            eb.Description += $"**Max Wild CP:** {maxWildCp}, **Max CP:** {maxCp} \r\n";

            if (pkmn.Types.Count > 0)
            {
                var types = new List<string>();
                pkmn.Types.ForEach(x =>
                {
                    if (Strings.TypeEmojis.ContainsKey(x.Type.ToLower()))
                    {
                        types.Add($"{Strings.TypeEmojis[x.Type.ToLower()]} {x.Type}");
                    }
                });
                eb.Description += $"**Types:** {string.Join("/", types)}\r\n";
            }

            if (float.TryParse(pokemon.Height, out float height) && float.TryParse(pokemon.Weight, out float weight))
            {
                var size = _db.GetSize(pokemon.Id, height, weight);
                eb.Description += $"**Size:** {size}\r\n";
            }

            var fastMove = _db.Movesets.ContainsKey(pokemon.FastMove) ? _db.Movesets[pokemon.FastMove] : null;
            if (fastMove != null)
            {
                //var fastMoveIcon = Strings.TypeEmojis.ContainsKey(fastMove.Type.ToLower()) ? Strings.TypeEmojis[fastMove.Type.ToLower()] : fastMove.Type;
                eb.Description += $"**Fast Move:** {fastMove.Name} ({fastMove.Type})\r\n";
            }

            var chargeMove = _db.Movesets.ContainsKey(pokemon.ChargeMove) ? _db.Movesets[pokemon.ChargeMove] : null;
            if (chargeMove != null)
            {
                //var chargeMoveIcon = Strings.TypeEmojis.ContainsKey(chargeMove.Type.ToLower()) ? Strings.TypeEmojis[chargeMove.Type.ToLower()] : chargeMove.Type;
                eb.Description += $"**Charge Move:** {chargeMove.Name} ({chargeMove.Type})\r\n";
            }

            eb.Description += $"**Location:** {Math.Round(pokemon.Latitude, 5)},{Math.Round(pokemon.Longitude, 5)}";
            eb.ImageUrl = string.Format(Strings.GoogleMapsStaticImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}";
            eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"versx | {DateTime.Now}"
            };
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
                _logger.Error($"Failed to lookup city for coordinates {raid.Latitude},{raid.Longitude}, skipping...");
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
                //Description = $"{pkmn.Name} raid available until {raid.EndTime.ToLongTimeString()}!",
                Url = string.Format(Strings.GoogleMaps, raid.Latitude, raid.Longitude),
                ImageUrl = string.Format(Strings.GoogleMapsStaticImage, raid.Latitude, raid.Longitude),
                ThumbnailUrl = string.Format(Strings.PokemonImage, raid.PokemonId, 0),
                Color = DiscordHelpers.BuildRaidColor(Convert.ToInt32(raid.Level))
            };

            var fixedEndTime = DateTime.Parse(raid.EndTime.ToLongTimeString());
            var remaining = GetRaidTimeRemaining(fixedEndTime);

            eb.Description = $"{pkmn.Name} Raid Ends: {raid.EndTime.ToLongTimeString()}\r\n\r\n";
            eb.Description += $"**Starts:** {raid.StartTime.ToLongTimeString()}\r\n";
            eb.Description += $"**Ends:** {raid.EndTime.ToLongTimeString()} ({remaining.ToReadableStringNoSeconds()} left)\r\n";

            var perfectRange = _db.GetPokemonCpRange(raid.PokemonId, 20);
            var boostedRange = _db.GetPokemonCpRange(raid.PokemonId, 25);
            eb.Description += $"**Perfect CP:** {perfectRange.Best} / :white_sun_rain_cloud: {boostedRange.Best}\r\n";

            if (pkmn.Types.Count > 0)
            {
                var types = new List<string>();
                pkmn.Types.ForEach(x =>
                {
                    if (Strings.TypeEmojis.ContainsKey(x.Type.ToLower()))
                    {
                        types.Add(Strings.TypeEmojis[x.Type.ToLower()] + " " + x.Type);
                    }
                });
                eb.Description += $"**Types:** {string.Join("/", types)}\r\n";
            }

            var fastMove = _db.Movesets.ContainsKey(raid.FastMove) ? _db.Movesets[raid.FastMove] : null;
            if (fastMove != null)
            {
                eb.Description += $"**Fast Move:** {Strings.TypeEmojis[fastMove.Type.ToLower()]} {fastMove.Name}\r\n";
            }

            var chargeMove = _db.Movesets.ContainsKey(raid.ChargeMove) ? _db.Movesets[raid.ChargeMove] : null;
            if (chargeMove != null)
            {
                eb.Description += $"**Charge Move:** {Strings.TypeEmojis[chargeMove.Type.ToLower()]} {chargeMove.Name}\r\n";
            }

            var strengths = new List<string>();
            var weaknesses = new List<string>();
            foreach (var type in pkmn.Types)
            {
                foreach (var strength in PokemonExtensions.GetStrengths(type.Type))
                {
                    if (!strengths.Contains(strength))
                    {
                        strengths.Add(strength);
                    }
                }
                foreach (var weakness in PokemonExtensions.GetWeaknesses(type.Type))
                {
                    if (!weaknesses.Contains(weakness))
                    {
                        weaknesses.Add(weakness);
                    }
                }
            }

            if (strengths.Count > 0)
            {
                eb.Description += $"**Strong Against:** {string.Join(", ", strengths)}\r\n";
            }

            if (weaknesses.Count > 0)
            {
                eb.Description += $"**Weaknesses:** {string.Join(", ", weaknesses)}\r\n";
            }

            eb.Description += $"**Location:** {Math.Round(raid.Latitude, 5)},{Math.Round(raid.Longitude, 5)}";
            eb.ImageUrl = string.Format(Strings.GoogleMapsStaticImage, raid.Latitude, raid.Longitude) + $"&key={_config.GmapsKey}";
            eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = $"versx | {DateTime.Now}"
            };
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