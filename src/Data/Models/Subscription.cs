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

        [XmlArrayItem("pokemonId")]
        [XmlArray("pokemonIds")]
        [JsonProperty("pokemonIds")]
        public List<uint> PokemonIds { get; set; }

        [XmlArrayItem("channelId")]
        [XmlArray("channelIds")]
        [JsonProperty("channelIds")]
        public List<ulong> ChannelIds { get; set; }

        [XmlAttribute("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        public Subscription()
        {
            PokemonIds = new List<uint>();
            ChannelIds = new List<ulong>();
            Enabled = true;
        }

        public Subscription(ulong userId, List<uint> pokemonIds, List<ulong> channels)
        {
            UserId = userId;
            PokemonIds = pokemonIds;
            ChannelIds = channels;
        }
    }
}