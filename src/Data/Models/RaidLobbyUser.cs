namespace BrockBot.Data.Models
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("raidLobbyUser")]
    [JsonObject("raidLobbyUser")]
    public class RaidLobbyUser
    {
        [XmlAttribute("userId")]
        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [XmlAttribute("isCheckedIn")]
        [JsonProperty("isCheckedIn")]
        public bool IsCheckedIn { get; set; }

        [XmlAttribute("isOnTheWay")]
        [JsonProperty("isOnTheWay")]
        public bool IsOnTheWay { get; set; }

        [XmlAttribute("userCount")]
        [JsonProperty("userCount")]
        public int UserCount { get; set; }

        [XmlAttribute("eta")]
        [JsonProperty("eta")]
        public string ETA { get; set; }

        [XmlAttribute("checkInTime")]
        [JsonProperty("checkInTime")]
        public DateTime CheckInTime { get; set; }

        [XmlAttribute("onTheWayTime")]
        [JsonProperty("onTheWayTime")]
        public DateTime OnTheWayTime { get; set; }

        public RaidLobbyUser()
        {
            CheckInTime = DateTime.MinValue;
            OnTheWayTime = DateTime.MinValue;
        }

        public RaidLobbyUser(ulong id, bool isCheckedIn, bool isOnTheWay, int userCount, string eta)
        {
            UserId = id;
            IsCheckedIn = isCheckedIn;
            IsOnTheWay = isOnTheWay;
            UserCount = userCount;
            ETA = eta;
        }
    }
}