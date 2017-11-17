namespace BrockBot.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("subscription")]
    [JsonObject("subscription")]
    public class Subscription
    {
        [XmlAttribute("userId")]
        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [XmlArrayItem("pokemon")]
        [XmlArray("pokemonSubscriptions")]
        [JsonProperty("pokemon")]
        //public List<Tuple<uint, int, int>> PokemonIds { get; set; }
        public List<Pokemon> Pokemon { get; set; }

        [XmlArrayItem("channelId")]
        [XmlArray("channelIds")]
        [JsonProperty("channelIds")]
        public List<ulong> ChannelIds { get; set; }

        [XmlAttribute("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        public Subscription()
        {
            //PokemonIds = new List<Tuple<uint, int, int>>();// List<uint>();
            Pokemon = new List<Pokemon>();
            ChannelIds = new List<ulong>();
            Enabled = true;
        }

        public Subscription(ulong userId, List<Pokemon> /*List<Tuple<uint, int, int>>List<uint>*/ pokemonIds, List<ulong> channels)
        {
            UserId = userId;
            Pokemon = pokemonIds;
            ChannelIds = channels;
        }
    }
}