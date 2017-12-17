namespace BrockBot.Net
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using Newtonsoft.Json;

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

        public const string PokemonImage = "https://raw.githubusercontent.com/Necrobot-Private/PokemonGO-Assets/master/pokemon/{0}.png";
        public const string GoogleMaps = "http://maps.google.com/maps?q={0},{1}";
        public const string GoogleMapsImage = "https://maps.googleapis.com/maps/api/staticmap?center={0},{1}&markers=color:red%7C&maptype=roadmap&size=250x125&zoom=15";

        #endregion

        #region Variables

        private DiscordClient _client;

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

        public HttpServer(ushort port)
        {
            var server = new HttpListener();
            server.Prefixes.Add($"http://127.0.0.1:{port}/");
            server.Prefixes.Add($"http://localhost:{port}/");
            server.Start();

            Log("Listening...");

            new System.Threading.Thread(x =>
            {
                Task.Run(async () =>
                {
                    await Start();

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
                });
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

                Log("Request: {0}", data);
                dynamic obj = JsonConvert.DeserializeObject(data);
                if (obj == null) return;

                foreach (dynamic part in obj)
                {
                    string type = Convert.ToString(part["type"]);
                    dynamic message = part["message"];
                    switch (type)
                    {
                        case "pokemon":
                            Task.Run(() => ParsePokemon(message));
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
                            Task.Run(() => ParseRaid(message));
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
                Log(ex);
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
                int disappearTime = Convert.ToInt32(Convert.ToString(message["disappear_time"]));
                string cp = Convert.ToString(message["cp"] ?? "?");
                string stamina = Convert.ToString(message["individual_stamina"] ?? "?");
                string attack = Convert.ToString(message["individual_attack"] ?? "?");
                string defense = Convert.ToString(message["individual_defense"] ?? "?");
                string gender = Convert.ToString(message["gender"] ?? "?");
                double latitude = Convert.ToDouble(Convert.ToString(message["latitude"]));
                double longitude = Convert.ToDouble(Convert.ToString(message["longitude"]));
                string playerLevel = Convert.ToString(message["player_level"] ?? "?");
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

                Log($"Pokemon Id: {pokeId}");
                Log($"Seconds Until Despawn: {TimeSpan.FromSeconds(secondsUntilDespawn)}");
                Log($"Disappear Time: {new DateTime(TimeSpan.FromMilliseconds(disappearTime).Ticks).ToLongTimeString()}");
                Log($"CP: {cp}");
                Log($"IV: {iv}");
                Log($"Stamina: {stamina}");
                Log($"Attack: {attack}");
                Log($"Defense: {defense}");
                Log($"Gender: {gender}");
                Log($"Level: {playerLevel}");
                Log($"Location: {latitude},{longitude}");
                Log($"Quick Move: {move1}");
                Log($"Charge Move: {move2}");
                Log($"Height: {height}");
                Log($"Weight: {weight}");
                Log($"Verified TTH: {verified}");

                var pokemon = new PokemonData
                (
                    pokeId,
                    cp,
                    iv,
                    stamina,
                    attack,
                    defense,
                    pokeGender,
                    playerLevel,
                    latitude,
                    longitude,
                    move1,
                    move2,
                    height,
                    weight,
                    new DateTime(TimeSpan.FromMilliseconds(disappearTime).Ticks), 
                    TimeSpan.FromSeconds(secondsUntilDespawn)
                );
                OnPokemonReceived(pokemon);

                //var embed = BuildEmbedPokemon(pokemon);//pokeId, cp, stamina, attack, defense, playerLevel, gender, latitude, longitude, new DateTime(TimeSpan.FromMilliseconds(disappearTime).Ticks), TimeSpan.FromSeconds(secondsUntilDespawn));
                //await SendMessage(wh, string.Empty, embed);
            }
            catch (Exception ex)
            {
                Log(ex);
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
                long start = Convert.ToInt64(Convert.ToString(message["start"]));
                long end = Convert.ToInt64(Convert.ToString(message["end"]));
                string level = Convert.ToString(message["level"] ?? "?");
                int pokemonId = Convert.ToInt32(Convert.ToString(message["pokemon_id"]));
                string cp = Convert.ToString(message["cp"]);
                string move1 = Convert.ToString(message["move_1"] ?? "?");
                string move2 = Convert.ToString(message["move_2"] ?? "?");

                Log($"Gym Id: {gymId}");
                Log($"Latitude: {latitude}");
                Log($"Longitude: {longitude}");
                Log($"Spawn Time: {new DateTime(spawn)}");
                Log($"Start Time: {new DateTime(start)}");
                Log($"End Time: {new DateTime(end)}");
                Log($"Level: {level}");
                Log($"Pokemon Id: {pokemonId}");
                Log($"CP: {cp}");
                Log($"Quick Move: {move1}");
                Log($"Charge Move: {move2}");

                var raid = new RaidData
                (
                    pokemonId,
                    level,
                    cp,
                    move1,
                    move2,
                    latitude,
                    longitude,
                    new DateTime(start),
                    new DateTime(end)
                );
                OnRaidReceived(raid);
            }
            catch (Exception ex)
            {
                Log(ex);
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
        //        Log(ex);
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
        //        Log(ex);
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
        //        Log(ex);
        //    }
        //}

        #endregion

        #region Private Methods

        private async Task Start()
        {
            if (_client != null)
            {
                Console.WriteLine($"WH already started, no need to start again.");
                return;
            }

            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                //DiscordBranch = Branch.Stable,
                LogLevel = LogLevel.Debug,
                Token = "Mzg0MjU0MDQ0NjkwMTg2MjU1.DPwIXA.bqkUMDRepWtutwpgq1EpmOM6JSM",//"MzY5MDQ4NzAzMDMxNTc0NTI5.DMS29Q.veATwRAOLhLPU6EexnokGhnInHM",//_config.AuthToken,
                TokenType = TokenType.Bot
            });

            _client.Ready += Client_Ready;
            _client.MessageCreated += Client_MessageCreated;
            //_client.DmChannelCreated += Client_DmChannelCreated;

            Console.WriteLine("Connecting to discord server...");
            await _client.ConnectAsync();

            //await Task.Delay(-1);
        }

        private async Task Client_Ready(ReadyEventArgs e)
        {
            //await SendMessage("https://discordapp.com/api/webhooks/374338702698348546/T47Tx-Rn7WGZ1ap99jTpJ8WDXi0AlBqNxr5593nhNEpNlzuAgqCP1Bj_WZs99hfa_11p", "Test", null);
            //var pokemonData = "[{\"message\": { \"disappear_time\": 1509308578, \"form\": null, \"seconds_until_despawn\": 1697, \"spawnpoint_id\": \"80c3347a811\", \"cp_multiplier\": null, \"move_2\": null, \"height\": null, \"time_until_hidden_ms\": -1773360683, \"last_modified_time\": 1509306881578, \"cp\": null, \"encounter_id\": \"MTQyODEwOTU4MDE4ODI3NDIyMjI=\", \"spawn_end\": 1378, \"move_1\": null, \"individual_defense\": null, \"verified\": true, \"weight\": null, \"pokemon_id\": 111, \"player_level\": 4, \"individual_stamina\": null, \"longitude\": -117.63445402991555, \"spawn_start\": 3179, \"gender\": 1, \"latitude\": 34.06371229679003, \"individual_attack\": null}, \"type\": \"pokemon\"}]";
            //var gymData = "[{\"message\": { \"raid_active_until\": 0, \"gym_id\": \"ZjFkNmI2ZGJiM2MwNGIzZDlmMDY3OWRiMTRmZmRjMWUuMTY=\", \"team_id\": 3, \"last_modified\": 1509293691305, \"slots_available\": 4, \"guard_pokemon_id\": 65, \"enabled\": true, \"longitude\": -117.647777, \"latitude\": 34.078344, \"lowest_pokemon_motivation\": 0.7790350317955017, \"total_cp\": 2331, \"occupied_since\": 1509291784}, \"type\": \"gym\"}]";
            //var raidData = "[{\"message\": {\"spawn\": 1509370079, \"move_1\": 240, \"move_2\": 103, \"end\": 1509377279, \"level\": 5, \"pokemon_id\": 244, \"gym_id\": \"M2Q5YjRlOTA1YWQzNGVhM2E5YmUyMjg3YzEzMzE4YTUuMTY=\", \"longitude\": -117.63476, \"start\": 1509373679, \"latitude\": 34.105152, \"cp\": 38628}, \"type\": \"raid\"}]";

            //ParseData(pokemonData);
            //ParseData(gymData);
            //ParseData(raidData);

            await Task.CompletedTask;
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            await Task.CompletedTask;
        }

        private async Task SendMessage(string webHookUrl, string message, DiscordEmbed embed = null)
        {
            var data = GetWebHookData(webHookUrl);
            if (data == null) return;

            var guildId = Convert.ToUInt64(Convert.ToString(data["guild_id"]));
            var channelId = Convert.ToUInt64(Convert.ToString(data["channel_id"]));

            var guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Guild does not exist!");
                Console.ResetColor();
                return;
            }

            var channel = await _client.GetChannelAsync(channelId);
            if (channel == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Channel does not exist!");
                Console.ResetColor();
                return;
            }

            await channel.SendMessageAsync(message, false, embed);
        }

        private dynamic GetWebHookData(string webHook)
        {
            /**Example:
             * {
             *   "name": "Pogo", 
             *   "channel_id": "352137087782486016", 
             *   "token": "fCdHsCZWeGB_vTkdPRqnB4_7fXil5tutXDLCZQYDurkTWQOqzSptiSQHbiCOBGlsF8J8", 
             *   "avatar": null, 
             *   "guild_id": "342025055510855680", 
             *   "id": "352137475101032449"
             * }
             * 
             */

            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                string json = wc.DownloadString(webHook);
                dynamic data = JsonConvert.DeserializeObject(json);
                return data;
            }
        }

        #endregion

        #region Logging Methods

        private void Log(string format, params object[] args)
        {
            Console.WriteLine(DateTime.Now.ToString() + " " + (args.Length > 0 ? string.Format(format, args) : format) + Environment.NewLine);
            File.AppendAllText("logs.txt", DateTime.Now.ToString() + " " + (args.Length > 0 ? string.Format(format, args) : format) + Environment.NewLine);
        }

        private void Log(Exception ex)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex);
            Console.ResetColor();
            Console.WriteLine();

            File.AppendAllText("errors.txt", ex + Environment.NewLine);
        }

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