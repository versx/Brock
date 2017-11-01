namespace PokeFilterBot.Data
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlRoot("subscription")]
    public class Subscription
    {
        [XmlAttribute("id")]
        public ulong UserId { get; set; }

        [XmlArrayItem("pokemonId")]
        [XmlArray("pokemonIds")]
        public List<uint> PokemonIds { get; set; }

        [XmlArrayItem("channel")]
        [XmlArray("channels")]
        public List<ulong> Channels { get; set; }

        [XmlElement("enabled")]
        public bool Enabled { get; set; }

        public Subscription()
        {
        }

        public Subscription(ulong userId, List<uint> pokemonIds, List<ulong> channels)
        {
            UserId = userId;
            PokemonIds = pokemonIds;
            Channels = channels;
        }
    }
}