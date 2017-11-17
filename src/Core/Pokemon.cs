namespace BrockBot
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("pokemon")]
    [JsonObject("pokemon")]
    public class Pokemon
    {
        [XmlAttribute("pokemonId")]
        [JsonProperty("pokemonId")]
        public uint PokemonId { get; set; }

        [XmlAttribute("pokemonName")]
        [JsonProperty("pokemonName")]
        public string PokemonName { get; set; }

        [XmlAttribute("minimumCP")]
        [JsonProperty("minimumCP")]
        public int MinimumCP { get; set; }

        [XmlAttribute("minimumIV")]
        [JsonProperty("minimumIV")]
        public int MinimumIV { get; set; }
    }
}