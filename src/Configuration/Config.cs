namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using BrockBot.Serialization;
    using BrockBot.Utilities;

    /// <summary>
    /// The main configuration file containing various important
    /// information in order for SEAgent to operate.
    /// </summary>
    [XmlRoot("config")]
    [JsonObject("config")]
    public class Config
    {
        #region Constants

        /// <summary>
        /// The default config file name with extension.
        /// </summary>
        public const string DefaultConfigFileName = /*"Config.xml"; */"config.json";

        private const string DefaultWelcomeMessage = "Hello {username}, welcome to versx's discord server!\r\nMy name is Brock and I'm here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies or even assigning yourself to a team or city role. To see a full list of my available commands please send me a direct message containing `.help`.";

        #endregion

        #region Properties

        [XmlElement("ownerId")]
        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        [XmlElement("adminCommandsChannelId")]
        [JsonProperty("adminCommandsChannelId")]
        public ulong AdminCommandsChannelId { get; set; }

        [XmlElement("commandsChannelId")]
        [JsonProperty("commandsChannelId")]
        public ulong CommandsChannelId { get; set; }

        [XmlElement("commandsPrefix")]
        [JsonProperty("commandsPrefix")]
        public char CommandsPrefix { get; set; }

        [XmlElement("sponsoredRaids")]
        [JsonProperty("sponsoredRaids")]
        public SponsoredRaidsConfig SponsoredRaids { get; set; }

        [XmlElement("apiVersion")]
        [JsonProperty("apiVersion")]
        public Version ScannerApiVersion { get; set; }

        [XmlElement("allowTeamAssignment")]
        [JsonProperty("allowTeamAssignment")]
        public bool AllowTeamAssignment { get; set; }

        [XmlElement("supporterRoleId")]
        [JsonProperty("supporterRoleId")]
        public ulong SupporterRoleId { get; set; }

        [XmlArray("teamRoles")]
        [XmlArrayItem("teamRole")]
        [JsonProperty("teamRoles")]
        public List<string> TeamRoles { get; set; }

        [XmlArray("cityRoles")]
        [XmlArrayItem("cityRole")]
        [JsonProperty("cityRoles")]
        public List<string> CityRoles { get; set; }

        [XmlElement("authToken")]
        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [XmlElement("webHookPort")]
        [JsonProperty("webHookPort")]
        public ushort WebHookPort { get; set; }

        [XmlElement("sendStartupMessage")]
        [JsonProperty("sendStartupMessage")]
        public bool SendStartupMessage { get; set; }

        [XmlArray("startupMessages")]
        [XmlArrayItem("startupMessage")]
        [JsonProperty("startupMessages")]
        public List<string> StartupMessages { get; set; }

        [XmlElement("startupMessageWebHook")]
        [JsonProperty("startupMessageWebHook")]
        public string StartupMessageWebHook { get; set; }

        [XmlElement("sendWelcomeMessage")]
        [JsonProperty("sendWelcomeMessage")]
        public bool SendWelcomeMessage { get; set; }

        [XmlElement("welcomeMessage")]
        [JsonProperty("welcomeMessage")]
        public string WelcomeMessage { get; set; }

        [XmlElement("notifyMemberJoined")]
        [JsonProperty("notifyMemberJoined")]
        public bool NotifyNewMemberJoined { get; set; }

        [XmlElement("notifyMemberLeft")]
        [JsonProperty("notifyMemberLeft")]
        public bool NotifyMemberLeft { get; set; }

        [XmlElement("notifyMemberBanned")]
        [JsonProperty("notifyMemberBanned")]
        public bool NotifyMemberBanned { get; set; }

        [XmlElement("notifyMemberUnbanned")]
        [JsonProperty("notifyMemberUnbanned")]
        public bool NotifyMemberUnbanned { get; set; }

        [XmlElement("twitterUpdates")]
        [JsonProperty("twitterUpdates")]
        public TwitterUpdatesConfig TwitterUpdates { get; set; }

        [XmlElement("advertisement")]
        [JsonProperty("advertisement")]
        public AdvertisementConfig Advertisement { get; set; }

        [XmlElement("nearbyNests")]
        [JsonProperty("nearbyNests")]
        public Dictionary<string, int> NearbyNests { get; set; }

        [XmlElement("encounterList")]
        [JsonProperty("encounterList")]
        public List<uint> EncounterList { get; set; }

        [XmlElement("customCommands")]
        [JsonProperty("customCommands")]
        public Dictionary<string, string> CustomCommands { get; set; }

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

        #region Constructor

        public Config()
        {
            AllowTeamAssignment = true;
            TeamRoles = new List<string>();
            //{
            //    "Valor",
            //    "Mystic",
            //    "Instinct"
            //};
            CityRoles = new List<string>();
            CommandsPrefix = '.';
            CustomCommands = new Dictionary<string, string>();
            EncounterList = new List<uint>();
            NearbyNests = new Dictionary<string, int>();
            NotifyMemberBanned = true;
            NotifyMemberUnbanned = true;
            NotifyNewMemberJoined = true;
            NotifyMemberLeft = true;
            OwnerId = 0;
            ScannerApiVersion = new Version(0, 87, 0);
            //SendStartupMessage = true;
            //StartupMessageWebHook = "";
            SendWelcomeMessage = true;
            StartupMessages = new List<string>();
            //{
            //    "Whoa, whoa...alright I'm awake.",
            //    "No need to push, I'm going...",
            //    "That was a weird dream, wait a minute...",
            //    //"Circuit overload, malfunktshun."
            //    "Circuits fully charged, let's do this!",
            //    "What is this place? How did I get here?",
            //    "Looks like we're not in Kansas anymore...",
            //    "Hey...watch where you put those mittens!"
            //};
            WebHookPort = 8008;
            WelcomeMessage = DefaultWelcomeMessage;
            SponsoredRaids = new SponsoredRaidsConfig();
            TwitterUpdates = new TwitterUpdatesConfig();
            Advertisement = new AdvertisementConfig();
        }

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
            return JsonConvert.SerializeObject(this, Formatting.Indented);
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
                //return XmlStringSerializer.Deserialize<Config>(data);
                return JsonStringSerializer.Deserialize<Config>(data);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return null;
        }

        public static Config CreateDefaultConfig(bool save = false)
        {
            var c = new Config
            {
                OwnerId = 0,
                AllowTeamAssignment = true,
                AuthToken = "",
                TeamRoles =
                {
                    "Valor",
                    "Mystic",
                    "Instinct"
                },
                CityRoles = new List<string>(),
                CommandsPrefix = '.',
                NotifyNewMemberJoined = true,
                NotifyMemberLeft = true,
                NotifyMemberBanned = true,
                NotifyMemberUnbanned = true,
                SendStartupMessage = true,
                SendWelcomeMessage = true
            };

            if (save)
            {
                c.Save();
            }
            return c;
        }

        #endregion
    }
}