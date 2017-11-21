namespace BrockBot.Data.Models
{
    using Newtonsoft.Json;

    [JsonObject("types")]
    public class PokemonType
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }
}