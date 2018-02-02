namespace BrockBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("subscription")]
    [JsonObject("subscription")]
    public class Subscription<T>
    {
        [JsonProperty("userId")]
        public ulong UserId { get; set; }
        
        [JsonProperty("pokemon")]
        public List<T> Pokemon { get; set; }
        
        [JsonProperty("raids")]
        public List<T> Raids { get; set; }
        
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("notificationsToday")]
        public ulong NotificationsToday { get; set; }

        [JsonIgnore]
        public bool Notified { get; set; }

        public Subscription()
        {
            Pokemon = new List<T>();
            Raids = new List<T>();
            Enabled = true;
        }

        public Subscription(ulong userId, List<T> pokemon, List<T> raids)
        {
            UserId = userId;
            Pokemon = pokemon;
            Raids = raids;
            Enabled = true;
        }

        public void ResetNotifications()
        {
            NotificationsToday = 0;
            Notified = false;
        }
    }
}