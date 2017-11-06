namespace PokeFilterBot.Data.Models
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot("lobby")]
    public class RaidLobby
    {
        [XmlAttribute("lobbyName")]
        public string LobbyName { get; set; }

        [XmlAttribute("channelId")]
        public ulong ChannelId { get; set; }

        [XmlAttribute("originalRaidMessageId")]
        public ulong OriginalRaidMessageId { get; set; }

        [XmlAttribute("pinnedRaidMessageId")]
        public ulong PinnedRaidMessageId { get; set; }

        [XmlAttribute("pokemonName")]
        public string PokemonName { get; set; }

        [XmlAttribute("startTime")]
        public DateTime StartTime { get; set; }

        [XmlAttribute("expireTime")]
        public DateTime ExpireTime { get; set; }

        [XmlAttribute("gymName")]
        public string GymName { get; set; }

        [XmlAttribute("address")]
        public string Address { get; set; }

        [XmlElement("checkInList")]
        public CheckInList UserCheckInList { get; set; }

        [XmlIgnore]
        public double MinutesLeft
        {
            get { return (ExpireTime - StartTime).TotalMinutes; }
        }

        [XmlIgnore]
        public bool IsExpired
        {
            get { return ExpireTime < DateTime.Now; }
        }

        [XmlIgnore]
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