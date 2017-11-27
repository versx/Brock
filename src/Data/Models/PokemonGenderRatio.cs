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

    //[JsonObject("genderRatio")]
    //public class PokemonGenderRatioOld
    //{
    //    [JsonProperty("M")]
    //    public double Male { get; set; }

    //    [JsonProperty("F")]
    //    public double Female { get; set; }
    //}
}