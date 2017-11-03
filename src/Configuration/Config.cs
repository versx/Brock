namespace PokeFilterBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using PokeFilterBot.Serialization;

    /// <summary>
    /// The main configuration file containing various important
    /// information in order for SEAgent to operate.
    /// </summary>
    [XmlRoot("config")]
    [JsonObject("config")]
    public class Config
    {
        /// <summary>
        /// The default config file name with extension.
        /// </summary>
        public const string DefaultConfigFileName = "Config.xml"; //"config.json";

        #region Variables

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
                    DefaultConfigFileName
                );
            }
        }

        #endregion

        #region Properties

        [XmlElement("authToken")]
        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [XmlElement("webHookUrl")]
        [JsonProperty("webHookUrl")]
        public string WebHookUrl { get; set; }

        #endregion

        #region Public Methods

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
            var serializedData = XmlStringSerializer.Serialize(this);
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

        #endregion

        #region Static Methods

        /// <summary>
        /// Loads the configuration file from the default path.
        /// </summary>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Config Load()
        {
            return Load(ConfigFilePath);
        }

        /// <summary>
        /// Loads the configuration file from the specified path.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        /// <returns>Returns the deserialized Config object.</returns>
        public static Config Load(string filePath)
        {
            try
            {
                var data = File.ReadAllText(filePath);
                return XmlStringSerializer.Deserialize<Config>(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadConfig: {ex}");
            }

            return null;
        }

        #endregion
    }
}