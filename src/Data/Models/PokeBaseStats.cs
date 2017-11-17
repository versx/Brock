namespace BrockBot.Data.Models
{
    using Newtonsoft.Json;

    [JsonObject("stats")]
    public class PokeBaseStats
    {
        [JsonProperty("attack")]
        public int Attack { get; set; }

        [JsonProperty("defense")]
        public int Defense { get; set; }

        [JsonProperty("stamina")]
        public int Stamina { get; set; }

        [JsonProperty("type1")]
        public string Type1 { get; set; }

        [JsonProperty("type2")]
        public string Type2 { get; set; }

        [JsonProperty("legendary")]
        public bool Legendary { get; set; }

        [JsonProperty("generation")]
        public int Generation { get; set; }

        [JsonProperty("weight")]
        public double Weight { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        public PokeBaseStats()
        {
        }
    }
}