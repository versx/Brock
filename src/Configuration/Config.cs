namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    using BrockBot.Data.Models;
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
        public const string DefaultConfigFileName = "config.json";

        //private const string DefaultWelcomeMessage = "Hello {username}, welcome to versx's discord server!\r\nMy name is Brock and I'm here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies or even assigning yourself to a team or city role. To see a full list of my available commands please send me a direct message containing `.help`.";
        private const string DefaultWelcomeMessage = @"Hello {username}, welcome to **versx**'s server!
My name is Brock and I'm here to assist you with certain things. Most commands that you'll send me will need to be sent to the #bot channel in the server but I can also process some commands via direct message.

First things first you might want to set your team, there are three available teams: Valor, Mystic, and Instinct. To set your team you'll want to use the `.team Valor/Mystic/Instinct` command, although this is optional. For more details please read the pinned message in the #bot channel titled Team Assignment.
Next you'll need to assign youself to some city feeds to see Pokemon spawns and Raids. Quickest way is to type the `.feedme all` command, otherwise please read the pinned message in the #bot channel titled City Feeds for more details.
Lastly if you'd like to get direct messages from me when a certain Pokemon with a specific IV percentage or raid appears, to do so please read the pinned message in the #bot channel titled Pokemon Notifications.

I will only send you direct message notifications of Pokemon or raids for city feeds that you are assigned to.
**To see a full list of my available commands please send me a direct message containing `.help`.**

Once you've completed the above steps you'll be all set to go catch those elusive monsters, be safe and have fun!";

        private const string RaidsFileName = "raids.txt";

        #endregion

        #region Properties

        #region Administration

        [JsonProperty("ownerId")]
        public ulong OwnerId { get; set; }

        [JsonProperty("adminCommandsChannelId")]
        public ulong AdminCommandsChannelId { get; set; }

        [JsonProperty("commandsChannelId")]
        public ulong CommandsChannelId { get; set; }

        [JsonProperty("commandsPrefix")]
        public char CommandsPrefix { get; set; }

        [JsonProperty("apiVersion")]
        public Version ScannerApiVersion { get; set; }

        [JsonProperty("authToken")]
        public string AuthToken { get; set; }

        [JsonProperty("gmapsKey")]
        public string GmapsKey { get; set; }

        [JsonProperty("webHookPort")]
        public ushort WebHookPort { get; set; }

        #endregion

        #region Role Assignment

        [JsonProperty("allowTeamAssignment")]
        public bool AllowTeamAssignment { get; set; }

        [JsonProperty("supporterRoleId")]
        public ulong SupporterRoleId { get; set; }

        [JsonProperty("teamEliteRoleId")]
        public ulong TeamEliteRoleId { get; set; }

        [JsonProperty("moderators")]
        public List<ulong> Moderators { get; set; }

        [JsonProperty("teamRoles")]
        public List<string> TeamRoles { get; set; }

        [JsonProperty("autoAssignNewMembersCityRoles")]
        public bool AssignNewMembersCityRoles { get; set; }

        [JsonProperty("cityRoles")]
        public List<string> CityRoles { get; set; }

        #endregion

        #region Startup Message

        [JsonProperty("sendStartupMessage")]
        public bool SendStartupMessage { get; set; }

        [JsonProperty("startupMessages")]
        public List<string> StartupMessages { get; set; }

        [JsonProperty("startupMessageWebHook")]
        public string StartupMessageWebHook { get; set; }

        #endregion

        #region Welcome Message

        [JsonProperty("sendWelcomeMessage")]
        public bool SendWelcomeMessage { get; set; }

        [JsonProperty("welcomeMessage")]
        public string WelcomeMessage { get; set; }

        #endregion

        #region Discord Events

        [JsonProperty("notifyMemberJoined")]
        public bool NotifyNewMemberJoined { get; set; }

        [JsonProperty("notifyMemberLeft")]
        public bool NotifyMemberLeft { get; set; }

        [JsonProperty("notifyMemberBanned")]
        public bool NotifyMemberBanned { get; set; }

        [JsonProperty("notifyMemberUnbanned")]
        public bool NotifyMemberUnbanned { get; set; }

        #endregion

        #region Services

        [JsonProperty("sponsoredRaids")]
        public List<SponsoredRaidsConfig> SponsoredRaids { get; set; }

        [JsonProperty("twitterUpdates")]
        public TwitterUpdatesConfig TwitterUpdates { get; set; }

        [JsonProperty("advertisement")]
        public AdvertisementConfig Advertisement { get; set; }

        [JsonProperty("nearbyNests")]
        public Dictionary<string, int> NearbyNests { get; set; }

        [JsonProperty("feedStatus")]
        public FeedStatusConfig FeedStatus { get; set; }

        [JsonProperty("supporters")]
        public Dictionary<ulong, Donator> Supporters { get; set; }

        [JsonProperty("customCommands")]
        public Dictionary<string, string> CustomCommands { get; set; }

        [JsonProperty("raidLobbies")]
        public RaidLobbyConfig RaidLobbies { get; set; }

        #endregion

        [JsonIgnore]
        public List<uint> RaidBosses { get; set; }

        [JsonProperty("mapFolder")]
        public string MapFolder { get; set; }

        [JsonProperty("geofenceFolder")]
        public string GeofenceFolder { get; set; }

        [JsonProperty("accuWeatherApiKey")]
        public string AccuWeatherApiKey { get; set; }

        [JsonProperty("giveawayChannelId")]
        public ulong GiveawayChannelId { get; set; }

        [JsonProperty("giveaways")]
        public List<Giveaway> Giveaways { get; set; }

        [JsonProperty("votingPollsChannelId")]
        public ulong VotingPollsChannelId { get; set; }

        [JsonProperty("onlySendEventPokemon")]
        public bool OnlySendEventPokemon { get; set; }

        [JsonProperty("eventPokemonMinimumIV")]
        public int EventPokemonMinimumIV { get; set; }

        [JsonProperty("eventPokemon")]
        public List<uint> EventPokemon { get; set; }

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
            //AssignNewMembersCityRoles = true;
            CityRoles = new List<string>();
            CommandsPrefix = '.';
            CustomCommands = new Dictionary<string, string>();
            EventPokemon = new List<uint>();
            NearbyNests = new Dictionary<string, int>();
            NotifyMemberBanned = true;
            NotifyMemberUnbanned = true;
            NotifyNewMemberJoined = true;
            NotifyMemberLeft = true;
            OwnerId = 0;
            //ScannerApiVersion = new Version(0, 87, 0);
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
            SponsoredRaids = new List<SponsoredRaidsConfig>();
            Supporters = new Dictionary<ulong, Donator>();
            RaidLobbies = new RaidLobbyConfig();
            TwitterUpdates = new TwitterUpdatesConfig();
            Advertisement = new AdvertisementConfig();
            FeedStatus = new FeedStatusConfig();

            Giveaways = new List<Giveaway>();
            RaidBosses = new List<uint>();
            var raidsFileName = Path.Combine(Data.Database.DataFolderName, RaidsFileName);
            var lines = File.ReadAllLines(raidsFileName);
            foreach (var line in lines)
            {
                if (!uint.TryParse(line, out uint bossId)) continue;

                if (!RaidBosses.Contains(bossId))
                {
                    RaidBosses.Add(bossId);
                }
            }
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