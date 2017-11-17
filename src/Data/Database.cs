namespace BrockBot.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using BrockBot.Data.Models;
    using BrockBot.Serialization;

    [XmlRoot("database")]
    [JsonObject("database")]
    public class Database
    {
        /// <summary>
        /// The default config file name with extension.
        /// </summary>
        public const string DefaultDatabaseFileName = "database.json";

        #region Properties

        [XmlArrayItem("server")]
        [XmlArray("servers")]
        [JsonProperty("servers")]
        public List<Server> Servers { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public List<PokedexItem> Pokemon { get; }

        /// <summary>
        /// Gets the config full config file path.
        /// </summary>
        [XmlIgnore]
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

        public Server this[ulong guildId]
        {
            get
            {
                if (ContainsKey(guildId))
                {
                    return Servers.Find(x => x.GuildId == guildId);
                }

                return null;
            }
        }

        #endregion

        #region Constructor

        public Database()
        {
            Servers = new List<Server>();

            if (File.Exists("pokemon_stats.json"))
            {
                var pokeDb = File.ReadAllText("pokemon_stats.json");
                if (!string.IsNullOrEmpty(pokeDb))
                {
                    Pokemon = JsonStringSerializer.Deserialize<List<PokedexItem>>(pokeDb);
                }
            }
        }

        #endregion

        #region Public Methods

        public bool ContainsKey(ulong guildId)
        {
            return Servers.Exists(x => x.GuildId == guildId);
        }

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

    [JsonObject("pokemon")]
    public class PokedexItem
    {
        [JsonProperty("id")]
        public uint Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("stats")]
        public PokeBaseStats Stats { get; set; }

        public PokedexItem()
        {
        }

        public PokedexItem(uint id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}