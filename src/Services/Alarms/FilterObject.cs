namespace BrockBot.Services.Alarms
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonObject("filter")]
    public class FilterObject
    {
        [JsonProperty("pokemon")]
        public List<int> Pokemon { get; set; }

        [JsonProperty("min_iv")]
        public uint MinimumIV { get; set; }

        [JsonProperty("max_iv")]
        public uint MaximumIV { get; set; }

        [JsonProperty("filters")]
        public FilterType FilterType { get; set; }

        [JsonProperty("ignoreMissing")]
        public bool IgnoreMissing { get; set; }

        public FilterObject()
        {
            Pokemon = new List<int>();
        }
    }
}