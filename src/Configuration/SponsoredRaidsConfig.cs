namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("sponsoredRaids")]
    [JsonObject("sponsoredRaids")]
    public class SponsoredRaidsConfig
    {
        [XmlArray("channelPool")]
        [XmlArrayItem("channel")]
        [JsonProperty("channelPool")]
        public List<ulong> ChannelPool { get; set; }

        [XmlArray("keywords")]
        [XmlArrayItem("sponsorRaidKeyword")]
        [JsonProperty("sponsorRaidKeywords")]
        public List<string> Keywords { get; set; }

        [XmlElement("webHook")]
        [JsonProperty("webHook")]
        public string WebHook { get; set; }

        public SponsoredRaidsConfig()
        {
            ChannelPool = new List<ulong>();
            Keywords = new List<string>();
        }
    }
}