namespace BrockBot.Data.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class PokemonInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("rarity")]
        public string Rarity { get; set; }

        [JsonProperty("spawn_rate")]
        public string SpawnRate { get; set; }

        [JsonProperty("types")]
        public List<PokemonType> Types { get; set; }

        [JsonProperty("base_stats")]
        public PokemonBaseStats BaseStats { get; set; }
    }
}