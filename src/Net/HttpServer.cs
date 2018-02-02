namespace BrockBot.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;

    using Newtonsoft.Json;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Utilities;

    public enum PokemonGender
    {
        Unset = 0,
        Male,
        Female,
        Genderless
    }

    public enum GymTeam
    {
        Uncontested = 0,
        Mystic,
        Valor,
        Instinct
    }

    public class HttpServer
    {
        #region Constants

        public const string PokemonImage = "https://bytebucket.org/anzmap/sprites/raw/388a1e0ef08b98eaa0412c8a5f67ffb14d6a707d/{0}.png";
        //https://raw.githubusercontent.com/Necrobot-Private/PokemonGO-Assets/master/pokemon/{0}.png
        //https://github.com/not4profit/images/tree/master/monsters/{0:D3}.png
        //https://bytebucket.org/anzmap/sprites/raw/388a1e0ef08b98eaa0412c8a5f67ffb14d6a707d/{0}.png
        public const string GoogleMaps = "http://maps.google.com/maps?q={0},{1}";
        public const string GoogleMapsImage = "https://maps.googleapis.com/maps/api/staticmap?center={0},{1}&markers=color:red%7C&maptype=roadmap&size=250x125&zoom=15";

        #endregion

        #region Variables

        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Events

        public event EventHandler<PokemonReceivedEventArgs> PokemonReceived;

        private void OnPokemonReceived(PokemonData pokemon)
        {
            PokemonReceived?.Invoke(this, new PokemonReceivedEventArgs(pokemon));
        }

        public event EventHandler<RaidReceivedEventArgs> RaidReceived;

        private void OnRaidReceived(RaidData raid)
        {
            RaidReceived?.Invoke(this, new RaidReceivedEventArgs(raid));
        }

        #endregion

        #region Constructor

        public HttpServer(Config config, IEventLogger logger)
        {
            _config = config;
            _logger = logger;

            var server = new HttpListener();
            try
            {
                //TODO: Requires administrative privileges
                var internalAddr = NetUtils.GetLocalIPv4();
                if (!server.Prefixes.Contains($"http://{internalAddr}:{_config.WebHookPort}/"))
                    server.Prefixes.Add($"http://{internalAddr}:{_config.WebHookPort}/");

                if (!server.Prefixes.Contains($"http://127.0.0.1:{_config.WebHookPort}/"))
                    server.Prefixes.Add($"http://127.0.0.1:{_config.WebHookPort}/");

                if (!server.Prefixes.Contains($"http://localhost:{_config.WebHookPort}/"))
                    server.Prefixes.Add($"http://localhost:{_config.WebHookPort}/");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
            server.Start();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Listening...");
            Console.ResetColor();

            new System.Threading.Thread(x =>
            {
                while (true)
                {
                    var context = server.GetContext();
                    var response = context.Response;

                    using (var sr = new StreamReader(context.Request.InputStream))
                    {
                        var data = sr.ReadToEnd();
                        ParseData(data);
                    }

                    var buffer = Encoding.UTF8.GetBytes("WH Test Running!");
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.Close();
                }
            })
            { IsBackground = true }.Start();
        }

        #endregion

        #region Data Parsing Methods

        private void ParseData(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return;

                File.AppendAllText("debug.txt", data + Environment.NewLine);

                //Log("Request: {0}", data);
                dynamic obj = JsonConvert.DeserializeObject(data);
                if (obj == null) return;

                foreach (dynamic part in obj)
                {
                    string type = Convert.ToString(part["type"]);
                    dynamic message = part["message"];
                    switch (type)
                    {
                        case "pokemon":
                            ParsePokemon(message);
                            break;
                        //case "gym":
                        //    ParseGym(message);
                        //    break;
                        //case "gym-info":
                        //case "gym_details":
                        //    ParseGymInfo(message);
                        //    break;
                        case "egg":
                        case "raid":
                            ParseRaid(message);
                            break;
                            //case "tth":
                            //case "scheduler":
                            //    ParseTth(message);
                            //    break;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);

                _logger.Error(ex);
                _logger.Info("{0}", Convert.ToString(data));
            }
        }

        private void ParsePokemon(dynamic message)
        {
            /*[{
             *  "message =
             *  {
             *      "disappear_time = 1509308578, 
             *      "form = null, 
             *      "seconds_until_despawn = 1697, 
             *      "spawnpoint_id = "80c3347a811", 
             *      "cp_multiplier = null,
             *      "move_2 = null, 
             *      "height = null, 
             *      "time_until_hidden_ms = -1773360683, 
             *      "last_modified_time = 1509306881578,
             *      "cp = null, 
             *      "encounter_id = "MTQyODEwOTU4MDE4ODI3NDIyMjI=", 
             *      "spawn_end = 1378, 
             *      "move_1 = null,
             *      "individual_defense = null, 
             *      "verified = true, 
             *      "weight = null, 
             *      "pokemon_id = 111, 
             *      "player_level = 4,
             *      "individual_stamina = null, 
             *      "longitude = -117.63445402991555, 
             *      "spawn_start = 3179,
             *      "gender = 1, 
             *      "latitude = 34.06371229679003, 
             *      "individual_attack = null
             *  }, 
             *  "type = "pokemon"
             *}]
             */

            try
            {
                int pokeId = Convert.ToInt32(Convert.ToString(message["pokemon_id"]));
                int secondsUntilDespawn = Convert.ToInt32(Convert.ToString(message["seconds_until_despawn"]));
                long disappearTime = Convert.ToInt64(Convert.ToString(message["disappear_time"]));
                string cp = Convert.ToString(message["cp"] ?? "?");
                string stamina = Convert.ToString(message["individual_stamina"] ?? "?");
                string attack = Convert.ToString(message["individual_attack"] ?? "?");
                string defense = Convert.ToString(message["individual_defense"] ?? "?");
                string gender = Convert.ToString(message["gender"] ?? "?");
                double latitude = Convert.ToDouble(Convert.ToString(message["latitude"]));
                double longitude = Convert.ToDouble(Convert.ToString(message["longitude"]));
                string level = Convert.ToString(message["pokemon_level"] ?? "?");
                string move1 = Convert.ToString(message["move_1"] ?? "?");
                string move2 = Convert.ToString(message["move_2"] ?? "?");
                string height = Convert.ToString(message["height"] ?? "?");
                string weight = Convert.ToString(message["weight"] ?? "?");
                bool verified = Convert.ToBoolean(Convert.ToString(message["verified"]));

                var iv = "?";
                if (!string.IsNullOrEmpty(stamina) && stamina != "?" &&
                    !string.IsNullOrEmpty(attack) && attack != "?" &&
                    !string.IsNullOrEmpty(defense) && defense != "?")
                {
                    int.TryParse(stamina, out int sta);
                    int.TryParse(attack, out int atk);
                    int.TryParse(defense, out int def);
                    iv = Convert.ToString((sta + atk + def) * 100 / 45) + "%";
                }

                if (string.IsNullOrEmpty(cp)) cp = "?";
                if (string.IsNullOrEmpty(stamina)) stamina = "?";
                if (string.IsNullOrEmpty(attack)) attack = "?";
                if (string.IsNullOrEmpty(defense)) defense = "?";
                if (string.IsNullOrEmpty(gender)) gender = "0";

                var pokeGender = (PokemonGender)Convert.ToInt32(gender);

                //Log($"Pokemon Id: {pokeId}");
                //Log($"Seconds Until Despawn: {TimeSpan.FromSeconds(secondsUntilDespawn)}");
                //Log($"Disappear Time: {new DateTime(TimeSpan.FromMilliseconds(disappearTime).Ticks).ToLongTimeString()}");
                //Log($"CP: {cp}");
                //Log($"IV: {iv}");
                //Log($"Stamina: {stamina}");
                //Log($"Attack: {attack}");
                //Log($"Defense: {defense}");
                //Log($"Gender: {gender}");
                //Log($"Level: {playerLevel}");
                //Log($"Location: {latitude},{longitude}");
                //Log($"Quick Move: {move1}");
                //Log($"Charge Move: {move2}");
                //Log($"Height: {height}");
                //Log($"Weight: {weight}");
                //Log($"Verified TTH: {verified}");

                var pokemon = new PokemonData
                (
                    pokeId,
                    cp,
                    iv,
                    stamina,
                    attack,
                    defense,
                    pokeGender,
                    level,
                    latitude,
                    longitude,
                    move1,
                    move2,
                    height,
                    weight,
                    Utils.FromUnix(disappearTime), 
                    TimeSpan.FromSeconds(secondsUntilDespawn)
                );
                OnPokemonReceived(pokemon);

                //var embed = BuildEmbedPokemon(pokemon);//pokeId, cp, stamina, attack, defense, playerLevel, gender, latitude, longitude, new DateTime(TimeSpan.FromMilliseconds(disappearTime).Ticks), TimeSpan.FromSeconds(secondsUntilDespawn));
                //await SendMessage(wh, string.Empty, embed);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);

                _logger.Error(ex);
                _logger.Info("{0}", Convert.ToString(message));
            }
        }

        private void ParseRaid(dynamic message)
        {
            try
            {
                //| Field | Details | Example | | ———— | —————————————————————– | ———— | 
                //gym_id | The gym’s unique ID | "NGY2ZjBjY2Y3OTUyNGQyZWFlMjc3ODkzODM2YmI1Y2YuMTY=" |
                //latitude | The gym’s latitude | 43.599321 |
                //longitude | The gym’s longitude | 5.181415 |
                //spawn | The time at which the raid spawned | 1500992342 |
                //start | The time at which the raid starts | 1501005600 |
                //end | The time at which the raid ends | 1501007400 |
                //level | The raid’s level | 5 |
                //pokemon_id | The raid boss’s ID | 249 |
                //cp | The raid boss’s CP | 42753 |
                //move_1 | The raid boss’s quick move | 274 |
                //move_2 | The raid boss’s charge move | 275 |
                string gymId = Convert.ToString(message["gym_id"]);
                double latitude = Convert.ToDouble(Convert.ToString(message["latitude"]));
                double longitude = Convert.ToDouble(Convert.ToString(message["longitude"]));
                long spawn = Convert.ToInt64(Convert.ToString(message["spawn"]));
                long start = Convert.ToInt64(Convert.ToString(message["start"]));//"raid_begin"]));
                long end = Convert.ToInt64(Convert.ToString(message["end"]));//"raid_end"]));
                string level = Convert.ToString(message["level"] ?? "?");

                if (message["pokemon_id"] == null)
                {
                    Console.WriteLine("Raid Egg found, skipping...");
                    return;
                }
                int pokemonId = Convert.ToInt32(Convert.ToString(message["pokemon_id"] ?? 0));
                string cp = Convert.ToString(message["cp"] ?? "?");
                string move1 = Convert.ToString(message["move_1"] ?? "?");
                string move2 = Convert.ToString(message["move_2"] ?? "?");

                //Log($"Gym Id: {gymId}");
                //Log($"Latitude: {latitude}");
                //Log($"Longitude: {longitude}");
                //Log($"Spawn Time: {new DateTime(spawn)}");
                //Log($"Start Time: {new DateTime(start)}");
                //Log($"End Time: {new DateTime(end)}");
                //Log($"Level: {level}");
                //Log($"Pokemon Id: {pokemonId}");
                //Log($"CP: {cp}");
                //Log($"Quick Move: {move1}");
                //Log($"Charge Move: {move2}");

                if (pokemonId == 0)
                {
                    _logger.Debug($"Level {level} Egg, skipping...");
                    return;
                }

                var raid = new RaidData
                (
                    pokemonId,
                    level,
                    cp,
                    move1,
                    move2,
                    latitude,
                    longitude,
                    Utils.FromUnix(start),
                    Utils.FromUnix(end)
                );
                OnRaidReceived(raid);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);

                _logger.Error(ex.StackTrace);
                _logger.Info("{0}", Convert.ToString(message));
            }
        }

        //private void ParseGym(dynamic message)
        //{
        //    try
        //    {
        //        long raidActiveUtil = Convert.ToInt64(Convert.ToString(message["raid_active_until"]));
        //        string gymId = Convert.ToString(message["gym_id"]);
        //        int teamId = Convert.ToInt32(Convert.ToString(message["team_id"]));
        //        long lastModified = Convert.ToInt64(Convert.ToString(message["last_modified"]));
        //        int slotsAvailable = Convert.ToInt32(Convert.ToString(message["slots_available"]));
        //        int guardPokemonId = Convert.ToInt32(Convert.ToString(message["guard_pokemon_id"]));
        //        bool enabled = Convert.ToBoolean(Convert.ToString(message["enabled"]));
        //        double latitude = Convert.ToDouble(Convert.ToString(message["latitude"]));
        //        double longitude = Convert.ToDouble(Convert.ToString(message["longitude"]));
        //        double lowestPokemonMotivation = Convert.ToDouble(Convert.ToString(message["lowest_pokemon_motivation"]));
        //        int totalCp = Convert.ToInt32(Convert.ToString(message["total_cp"]));
        //        long occupiedSince = Convert.ToInt64(Convert.ToString(message["occupied_since"]));

        //        Log($"Raid Active Until: {new DateTime(TimeSpan.FromSeconds(raidActiveUtil).Ticks)}");
        //        Log($"Gym Id: {gymId}");
        //        Log($"Team Id: {teamId}");
        //        Log($"Last Modified: {lastModified}");
        //        Log($"Slots Available: {slotsAvailable}");
        //        Log($"Guard Pokemon Id: {guardPokemonId}");
        //        Log($"Enabled: {enabled}");
        //        Log($"Latitude: {latitude}");
        //        Log($"Longitude: {longitude}");
        //        Log($"Lowest Pokemon Motivation: {lowestPokemonMotivation}");
        //        Log($"Total CP: {totalCp}");
        //        Log($"Occupied Since: {new DateTime(TimeSpan.FromSeconds(occupiedSince).Ticks)}");//new DateTime(occupiedSince)}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //    }
        //}

        //private void ParseGymInfo(dynamic message)
        //{
        //    try
        //    {
        //        //| Field | Details | Example | | ————- | —————————————————— | ———————– |
        //        //id | The gym’s unique ID | "MzcwNGE0MjgyNThiNGE5NWFkZWIwYTBmOGM1Yzc2ODcuMTE=" |
        //        //name | The gym’s name | "St.Clements Church" |
        //        //description | The gym’s description | "" |
        //        //url | A URL to the gym’s image | "http://lh3.googleusercontent.com/image_url" |
        //        //latitude | The gym’s latitude | 43.633181 |
        //        //longitude | The gym’s longitude | 5.296836 |
        //        //team | The team that currently controls the gym | 1 |
        //        //pokemon | An array containing the Pokémon currently in the gym | [] |

        //        string id = Convert.ToString(message["id"]);
        //        string name = Convert.ToString(message["name"]);
        //        string description = Convert.ToString(message["description"]);
        //        string url = Convert.ToString(message["url"]);
        //        double latitude = Convert.ToDouble(Convert.ToString(message["latitude"]));
        //        double longitude = Convert.ToDouble(Convert.ToString(message["longitude"]));
        //        int team = Convert.ToInt32(Convert.ToString(message["team"]));
        //        dynamic pokemon = message["pokemon"];

        //        //Gym Pokémon:
        //        //Field | Details | Example | | ————————– | ——————————————————– | ———— |
        //        //trainer_name | The name of the trainer that the Pokémon belongs to | "johndoe9876" |
        //        //trainer_level | The trainer’s level1 | 34 |
        //        //pokemon_uid | The Pokémon’s unique ID | 4348002772281054056 |
        //        //pokemon_id | The Pokémon’s ID | 242 |
        //        //cp | The Pokémon’s base CP | 2940 |
        //        //cp_decayed | The Pokémon’s current CP | 115 |
        //        //stamina_max | The Pokémon’s max stamina | 500 |
        //        //stamina | The Pokémon’s current stamina | 500 |
        //        //move_1 | The Pokémon’s quick move | 234 |
        //        //move_2 | The Pokémon’s charge move | 108 |
        //        //height | The Pokémon’s height | 1.746612787246704 |
        //        //weight | The Pokémon’s weight | 51.84344482421875 |
        //        //form | The Pokémon’s form | 0 |
        //        //iv_attack | The Pokémon’s attack IV | 12 |
        //        //iv_defense | The Pokémon’s defense IV | 14 |
        //        //iv_stamina | The Pokémon’s stamina IV | 14 |
        //        //cp_multiplier | The Pokémon’s CP multiplier | 0.4785003960132599 |
        //        //additional_cp_multiplier | The Pokémon’s additional CP multiplier | 0.0 |
        //        //num_upgrades | The number of times that the Pokémon has been powered up | 31 |
        //        //deployment_time | The time at which the Pokémon was added to the gym | 1504361277 |

        //        Log($"Gym id: {id}");
        //        Log($"Name: {name}");
        //        Log($"Description: {description}");
        //        Log($"Url: {url}");
        //        Log($"Latitude: {latitude}");
        //        Log($"Longitude: {longitude}");
        //        Log($"Team: {team}");
        //        Log($"Pokemon:");

        //        foreach (var pkmn in pokemon)
        //        {
        //            string trainerName = Convert.ToString(pkmn["trainer_name"]);
        //            int trainerLevel = Convert.ToInt32(Convert.ToString(pkmn["trainer_level"]));
        //            string pokemonUid = Convert.ToString(pkmn["pokemon_uid"]);
        //            int pokemonId = Convert.ToInt32(Convert.ToString(pkmn["pokemon_id"]));
        //            int cp = Convert.ToInt32(Convert.ToString(pkmn["cp"]));
        //            int cpDecayed = Convert.ToInt32(Convert.ToString(pkmn["cp_decayed"]));
        //            int staminaMax = Convert.ToInt32(Convert.ToString(pkmn["stamina_max"]));
        //            int stamina = Convert.ToInt32(Convert.ToString(pkmn["stamina"]));
        //            string move1 = Convert.ToString(pkmn["move_1"] ?? "?");
        //            string move2 = Convert.ToString(pkmn["move_2"] ?? "?");
        //            double height = Convert.ToDouble(Convert.ToString(pkmn["height"]));
        //            double weight = Convert.ToDouble(Convert.ToString(pkmn["weight"]));
        //            int form = Convert.ToInt32(Convert.ToString(pkmn["form"]));
        //            int ivStamina = Convert.ToInt32(Convert.ToString(pkmn["iv_stamina"]));
        //            int ivAttack = Convert.ToInt32(Convert.ToString(pkmn["iv_attack"]));
        //            int ivDefense = Convert.ToInt32(Convert.ToString(pkmn["iv_defense"]));
        //            double cpMultiplier = Convert.ToDouble(Convert.ToString(pkmn["cp_multiplier"]));
        //            double additionalCpMultiplier = Convert.ToDouble(Convert.ToString(pkmn["additional_cp_multiplier"]));
        //            int numUpgrades = Convert.ToInt32(Convert.ToString(pkmn["num_upgrades"]));
        //            long deploymentTime = Convert.ToInt64(Convert.ToString(pkmn["deployment_time"]));

        //            Log($"Trainer Name: {trainerName}");
        //            Log($"Trainer Level: {trainerLevel}");
        //            Log($"Pokemon Uid: {pokemonUid}");
        //            Log($"Pokemon Id: {pokemonId}");
        //            Log($"CP: {cp}");
        //            Log($"CP Decayed: {cpDecayed}");
        //            Log($"Stamina: {stamina}");
        //            Log($"Stamina Max: {staminaMax}");
        //            Log($"Quick Move: {move1}");
        //            Log($"Charge Move: {move2}");
        //            Log($"Height: {height}");
        //            Log($"Width: {weight}");
        //            Log($"Form: {form}");
        //            Log($"IV Stamina: {ivStamina}");
        //            Log($"IV Attack: {ivAttack}");
        //            Log($"IV Defense: {ivDefense}");
        //            Log($"CP Multiplier: {cpMultiplier}");
        //            Log($"Additional CP Multiplier: {additionalCpMultiplier}");
        //            Log($"Number of Upgrades: {numUpgrades}");
        //            Log($"Deployment Time: {new DateTime(TimeSpan.FromSeconds(deploymentTime).Ticks)}");
        //            Log(Environment.NewLine);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //    }
        //}

        //private void ParseTth(dynamic message)
        //{
        //    try
        //    {
        //        //Field | Details | Example | | ————– | ———————————————————– | ————- |
        //        //name | The type of scheduler which performed the update | "SpeedScan" |
        //        //instance | The status name of scan instance which performed the update | "New York" |
        //        //tth_found | The completion status of the TTH scan | 0.9965 |
        //        //spawns_found | The number of spawns found in this update | 5 |
        //        string name = Convert.ToString(message["name"]);
        //        string instance = Convert.ToString(message["instance"]);
        //        double tthFound = Convert.ToDouble(Convert.ToString(message["tth_found"]));
        //        int spawnsFound = Convert.ToInt32(Convert.ToString(message["spawns_found"]));

        //        Log($"Name: {name}");
        //        Log($"Instance: {instance}");
        //        Log($"Tth Found: {tthFound}");
        //        Log($"Spawns Found: {spawnsFound}");
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //    }
        //}

        #endregion
    }

    public class PokemonReceivedEventArgs : EventArgs
    {
        public PokemonData Pokemon { get; }

        public PokemonReceivedEventArgs(PokemonData pokemon)
        {
            Pokemon = pokemon;
        }
    }

    public class PokemonData
    {
        public int PokemonId { get; }

        public string CP { get; }

        public string IV { get; }

        public string Stamina { get; }

        public string Attack { get; }

        public string Defense { get; }

        public PokemonGender Gender { get; }

        public string PlayerLevel { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public string FastMove { get; }

        public string ChargeMove { get; }

        public string Height { get; }

        public string Weight { get; }

        public DateTime DespawnTime { get; }

        public TimeSpan SecondsLeft { get; }

        public PokemonData(int pokemonId, string cp, string iv, string stamina, string attack, string defense, PokemonGender gender, string level, double lat, double lng, string move1, string move2, string height, string weight, DateTime despawn, TimeSpan secondsLeft)
        {
            PokemonId = pokemonId;
            CP = cp;
            IV = iv;
            Stamina = stamina;
            Attack = attack;
            Defense = defense;
            Gender = gender;
            PlayerLevel = level;
            Latitude = lat;
            Longitude = lng;
            FastMove = move1;
            ChargeMove = move2;
            Height = height;
            Weight = weight;
            DespawnTime = despawn;
            SecondsLeft = secondsLeft;
        }
    }

    public class RaidReceivedEventArgs : EventArgs
    {
        public RaidData Raid { get; }

        public RaidReceivedEventArgs(RaidData raid)
        {
            Raid = raid;
        }
    }

    public class RaidData
    {
        public int PokemonId { get; }

        public string Level { get; }

        public string CP { get; }

        public string FastMove { get; }

        public string ChargeMove { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public RaidData(int pokemonId, string level, string cp, string move1, string move2, double lat, double lng, DateTime startTime, DateTime endTime)
        {
            PokemonId = pokemonId;
            Level = level;
            CP = cp;
            FastMove = move1;
            ChargeMove = move2;
            Latitude = lat;
            Longitude = lng;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}