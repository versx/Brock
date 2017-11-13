namespace BrockBot.Data.Models
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("lobby")]
    [JsonObject("lobby")]
    public class RaidLobby
    {
        [XmlAttribute("lobbyName")]
        [JsonProperty("lobbyName")]
        public string LobbyName { get; set; }

        [XmlAttribute("channelId")]
        [JsonProperty("channelId")]
        public ulong ChannelId { get; set; }

        [XmlAttribute("originalRaidMessageId")]
        [JsonProperty("originalRaidMessageId")]
        public ulong OriginalRaidMessageId { get; set; }

        [XmlAttribute("pinnedRaidMessageId")]
        [JsonProperty("pinnedRaidMessageId")]
        public ulong PinnedRaidMessageId { get; set; }

        [XmlAttribute("pokemonName")]
        [JsonProperty("pokemonName")]
        public string PokemonName { get; set; }

        [XmlAttribute("startTime")]
        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [XmlAttribute("expireTime")]
        [JsonProperty("expireTime")]
        public DateTime ExpireTime { get; set; }

        [XmlAttribute("gymName")]
        [JsonProperty("gymName")]
        public string GymName { get; set; }

        [XmlAttribute("address")]
        [JsonProperty("address")]
        public string Address { get; set; }

        [XmlElement("checkInList")]
        [JsonProperty("checkInList")]
        public CheckInList UserCheckInList { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public double MinutesLeft
        {
            get { return (ExpireTime - StartTime).TotalMinutes; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public bool IsExpired
        {
            get { return ExpireTime <= DateTime.Now; }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int NumUsersOnTheWay
        {
            get
            {
                int usersOnTheWay = 0;
                UserCheckInList.ForEach(x =>
                {
                    if (x.IsOnTheWay && !x.IsCheckedIn)
                    {
                        usersOnTheWay += x.UserCount;
                    }
                });
                return usersOnTheWay;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        public int NumUsersCheckedIn
        {
            get
            {
                int usersCheckedIn = 0;
                UserCheckInList.ForEach(x =>
                {
                    if (!x.IsOnTheWay && x.IsCheckedIn)
                    {
                        usersCheckedIn += x.UserCount;
                    }
                });
                return usersCheckedIn;
            }
        }

        public RaidLobby()
        {
            StartTime = DateTime.MinValue;
            ExpireTime = DateTime.MinValue;
            UserCheckInList = new CheckInList();
        }

        public RaidLobby(string lobbyName, ulong originalRaidMessageId) : this()
        {
            LobbyName = lobbyName;
            OriginalRaidMessageId = originalRaidMessageId;
        }
    }
}