#region Old Raid Lobby System
//namespace BrockBot.Data.Models
//{
//    using System;
//    using System.Collections.Generic;
//    using System.Xml.Serialization;

//    using Newtonsoft.Json;

//    [XmlRoot("checkInList")]
//    [JsonObject("checkInList")]
//    public class CheckInList : List<RaidLobbyUser>
//    {
//        [XmlIgnore]
//        [JsonIgnore]
//        public RaidLobbyUser this[ulong userId]
//        {
//            get
//            {
//                return Find(x => x.UserId == userId);
//            }
//        }

//        public bool ContainsKey(ulong userId)
//        {
//            return this[userId] != null;
//        }
//    }
//}
#endregion