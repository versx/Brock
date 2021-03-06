﻿namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("sponsoredRaids")]
    [JsonObject("sponsoredRaids")]
    public class SponsoredRaidsConfig
    {
        [XmlElement("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [XmlArray("channelPool")]
        [XmlArrayItem("channel")]
        [JsonProperty("channelPool")]
        public List<ulong> ChannelPool { get; set; }

        [XmlArray("keywords")]
        [XmlArrayItem("keyword")]
        [JsonProperty("keywords")]
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