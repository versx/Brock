namespace BrockBot.Configuration
{
    using System;

    using Newtonsoft.Json;

    [JsonObject("giveaway")]
    public class Giveaway
    {
        [JsonProperty("pokemonId")]
        public uint PokemonId { get; set; }

        [JsonProperty("winner")]
        public ulong Winner { get; set; }

        [JsonProperty("startTime")]
        public DateTime StartTime { get; set; }

        [JsonProperty("started")]
        public bool Started { get; set; }
    }
}