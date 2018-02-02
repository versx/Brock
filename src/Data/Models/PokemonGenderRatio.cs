namespace BrockBot.Data.Models
{
    using Newtonsoft.Json;

    [JsonObject("gender_ratio")]
    public class PokemonGenderRatio
    {
        [JsonProperty("male")]
        public double Male { get; set; }

        [JsonProperty("female")]
        public double Female { get; set; }
    }
}