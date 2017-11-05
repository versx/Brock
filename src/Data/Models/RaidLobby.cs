namespace PokeFilterBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("lobby")]
    public class RaidLobby
    {
        [XmlElement("lobbyName")]
        public string LobbyName { get; set; }

        [XmlElement("raidMessageId")]
        public ulong RaidMessageId { get; set; }

        [XmlElement("pokemonName")]
        public string PokemonName { get; set; }

        [XmlElement("startTime")]
        public DateTime StartTime { get; set; }

        [XmlElement("expireTime")]
        public DateTime ExpireTime { get; set; }

        [XmlElement("checked_in")]
        public List<ulong> PlayersCheckedIn { get; set; }

        public RaidLobby()
        {
        }

        public RaidLobby(string lobbyName, ulong raidMessageId)
        {
            LobbyName = lobbyName;
            RaidMessageId = raidMessageId;
        }
    }
}