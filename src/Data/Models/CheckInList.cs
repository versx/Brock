namespace PokeFilterBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("checkInList")]
    public class CheckInList : List<RaidLobbyUser>
    {
        [XmlIgnore]
        public RaidLobbyUser this[ulong userId]
        {
            get
            {
                return Find(x => x.UserId == userId);
            }
        }

        public bool ContainsKey(ulong userId)
        {
            return this[userId] != null;
        }
    }
}