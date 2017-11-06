namespace PokeFilterBot.Data.Models
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot("raidLobbyUser")]
    public class RaidLobbyUser
    {
        [XmlAttribute("userId")]
        public ulong UserId { get; set; }

        [XmlAttribute("isCheckedIn")]
        public bool IsCheckedIn { get; set; }

        [XmlAttribute("isOnTheWay")]
        public bool IsOnTheWay { get; set; }

        [XmlAttribute("userCount")]
        public int UserCount { get; set; }

        [XmlAttribute("eta")]
        public string ETA { get; set; }

        [XmlAttribute("checkInTime")]
        public DateTime CheckInTime { get; set; }

        [XmlAttribute("onTheWayTime")]
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