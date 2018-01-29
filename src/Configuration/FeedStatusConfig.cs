namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    [JsonObject("feedStatus")]
    public class FeedStatusConfig
    {
        [JsonProperty("channels")]
        public List<ulong> Channels { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        public FeedStatusConfig()
        {
            Channels = new List<ulong>();
            Enabled = true;
        }
    }
}