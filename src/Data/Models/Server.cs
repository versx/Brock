//namespace BrockBot.Data.Models
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Xml.Serialization;

//    using Newtonsoft.Json;

//    [XmlRoot("server")]
//    [JsonObject("server")]
//    public class Server
//    {
//        #region Properties

//        [XmlElement("guildId")]
//        [JsonProperty("guildId")]
//        public ulong GuildId { get; set; }

//        [XmlArrayItem("lobby")]
//        [XmlArray("lobbies")]
//        [JsonProperty("lobbies")]
//        public List<RaidLobby> Lobbies { get; set; }

//        [XmlElement("subscriptions")]
//        [JsonProperty("subscriptions")]
//        public List<Subscription<Pokemon>> Subscriptions { get; set; }

//        [XmlIgnore]
//        [JsonIgnore]
//        public Subscription<Pokemon> this[ulong userId]
//        {
//            get { return Subscriptions.Find(x => x.UserId == userId); }
//        }

//        #endregion

//        #region Constructor(s)

//        public Server()
//        {
//            Lobbies = new List<RaidLobby>();
//            Subscriptions = new List<Subscription<Pokemon>>();
//        }

//        public Server(ulong guildId, List<RaidLobby> lobbies, List<Subscription<Pokemon>> subscriptions)
//        {
//            GuildId = guildId;
//            Lobbies = lobbies;
//            Subscriptions = subscriptions;
//        }

//        #endregion

//        #region Public Methods

//        public bool SubscriptionExists(ulong userId)
//        {
//            return this[userId] != null;
//        }

//        public bool RemoveAllPokemon(ulong userId)
//        {
//            if (SubscriptionExists(userId))
//            {
//                var sub = this[userId];
//                sub.Pokemon.Clear();
//                return true;
//            }

//            return false;
//        }

//        public bool RemoveAllRaids(ulong userId)
//        {
//            if (SubscriptionExists(userId))
//            {
//                var sub = this[userId];
//                sub.Raids.Clear();
//            }

//            return false;
//        }

//        #endregion
//    }
//}