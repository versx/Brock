namespace BrockBot.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using BrockBot.Data.Models;
    using BrockBot.Serialization;
    using BrockBot.Services;

    [XmlRoot("database")]
    [JsonObject("database")]
    public class Database : IDatabase
    {
        /// <summary>
        /// Default main database file name with extension.
        /// </summary>
        public const string DefaultDatabaseFileName = "database.json";

        /// <summary>
        /// Default Pokemon database file name with extension.
        /// </summary>
        public const string PokemonDatabaseFileName = "pokemon.json";

        /// <summary>
        /// Default Pokemon moveset database file name with extension.
        /// </summary>
        public const string MovesetDatabaseFileName = "moves.json";

        #region Properties

        [JsonProperty("lastChecked")]
        public DateTime LastChecked { get; set; }

        [JsonProperty("lobbies")]
        public List<RaidLobby> Lobbies { get; set; }

        [JsonProperty("subscriptions")]
        public List<Subscription<Pokemon>> Subscriptions { get; set; }

        [JsonProperty("reminders")]
        public ConcurrentDictionary<ulong, List<Reminder>> Reminders { get; set; }

        [JsonIgnore]
        public Dictionary<string, PokemonInfo> Pokemon { get; }

        [JsonIgnore]
        public Dictionary<string, Moveset> Movesets { get; }

        /// <summary>
        /// Gets the config full config file path.
        /// </summary>
        [JsonIgnore]
        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine
                (
                    Directory.GetCurrentDirectory(),
                    DefaultDatabaseFileName
                );
            }
        }

        [JsonIgnore]
        public Subscription<Pokemon> this[ulong userId]
        {
            get { return Subscriptions.Find(x => x.UserId == userId); }
        }

        #endregion

        #region Constructor

        public Database()
        {
            Reminders = new ConcurrentDictionary<ulong, List<Reminder>>();
            Subscriptions = new List<Subscription<Pokemon>>();

            if (File.Exists(PokemonDatabaseFileName))
            {
                var pokeDb = File.ReadAllText(PokemonDatabaseFileName);
                if (!string.IsNullOrEmpty(pokeDb))
                {
                    Pokemon = JsonStringSerializer.Deserialize<Dictionary<string, PokemonInfo>>(pokeDb);
                }
            }

            if (File.Exists(MovesetDatabaseFileName))
            {
                var movesDb = File.ReadAllText(MovesetDatabaseFileName);
                if (!string.IsNullOrEmpty(movesDb))
                {
                    Movesets = JsonStringSerializer.Deserialize<Dictionary<string, Moveset>>(movesDb);
                }
            }
        }

        #endregion

        #region Public Methods

        public bool SubscriptionExists(ulong userId)
        {
            return this[userId] != null;
        }

        public bool RemoveAllPokemon(ulong userId)
        {
            if (SubscriptionExists(userId))
            {
                var sub = this[userId];
                sub.Pokemon.Clear();
                return true;
            }

            return false;
        }

        public bool RemoveAllRaids(ulong userId)
        {
            if (SubscriptionExists(userId))
            {
                var sub = this[userId];
                sub.Raids.Clear();
            }

            return false;
        }

        public uint PokemonIdFromName(string name)
        {
            foreach (var poke in Pokemon)
            {
                if (poke.Value.Name.Contains(name))
                    return Convert.ToUInt32(poke.Key);
            }

            return 0;
        }

        public string PokemonNameFromId(int id)
        {
            foreach (var poke in Pokemon)
            {
                if (poke.Key == id.ToString())
                    return poke.Value.Name;
            }

            return null;
        }

        //public bool ContainsKey(ulong guildId)
        //{
        //    return Servers.Exists(x => x.GuildId == guildId);
        //}

        /// <summary>
        /// Saves the configuration file to the default path.
        /// </summary>
        public void Save()
        {
            Save(ConfigFilePath);
        }

        /// <summary>
        /// Saves the configuration file to the specified path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        public void Save(string filePath)
        {
            //var serializedData = XmlStringSerializer.Serialize(this);
            var serializedData = JsonStringSerializer.Serialize(this);
            File.WriteAllText(filePath, serializedData);
        }

        /// <summary>
        /// Serializes the Config object to an xml string using
        /// the <seealso cref="XmlStringSerializer"/> class.
        /// </summary>
        /// <returns>Returns an xml string representing this object.</returns>
        public string ToXmlString()
        {
            return XmlStringSerializer.Serialize(this);
        }

        public string ToJsonString()
        {
            return JsonStringSerializer.Serialize(this);
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// Loads the configuration file from the default path.
        /// </summary>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Database Load()
        {
            return Load(ConfigFilePath);
        }

        /// <summary>
        /// Loads the configuration file from the specified path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Database Load(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var data = File.ReadAllText(filePath);
                    //return XmlStringSerializer.Deserialize<Database>(data);
                    return JsonStringSerializer.Deserialize<Database>(data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadConfig: {ex}");
            }

            return new Database();
        }

        #endregion
    }
}