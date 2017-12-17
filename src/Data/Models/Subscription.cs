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
        [XmlAttribute("userId")]
        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [XmlArrayItem("pokemon")]
        [XmlArray("pokemonSubscriptions")]
        [JsonProperty("pokemon")]
        public List<T> Pokemon { get; set; }

        [XmlArrayItem("raid")]
        [XmlArray("raidSubscriptions")]
        [JsonProperty("raids")]
        public List<T> Raids { get; set; }
        
        [XmlAttribute("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

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
    }
}