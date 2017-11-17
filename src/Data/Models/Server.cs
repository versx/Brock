namespace BrockBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("server")]
    [JsonObject("server")]
    public class Server
    {
        #region Properties

        [XmlElement("guildId")]
        [JsonProperty("guildId")]
        public ulong GuildId { get; set; }

        [XmlArrayItem("lobby")]
        [XmlArray("lobbies")]
        [JsonProperty("lobbies")]
        public List<RaidLobby> Lobbies { get; set; }

        [XmlElement("subscriptions")]
        [JsonProperty("subscriptions")]
        public List<Subscription> Subscriptions { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public Subscription this[ulong userId]
        {
            get { return Subscriptions.Find(x => x.UserId == userId); }
        }

        #endregion

        #region Constructor(s)

        public Server()
        {
            Lobbies = new List<RaidLobby>();
            Subscriptions = new List<Subscription>();
        }

        public Server(ulong guildId, List<RaidLobby> lobbies, List<Subscription> subscriptions)
        {
            GuildId = guildId;
            Lobbies = lobbies;
            Subscriptions = subscriptions;
        }

        #endregion

        #region Public Methods

        public bool ContainsKey(ulong userId)
        {
            return this[userId] != null;
        }

        public bool Remove(ulong userId)
        {
            if (ContainsKey(userId))
            {
                return Subscriptions.Remove(this[userId]);
            }

            return false;
        }

        #endregion
    }
}