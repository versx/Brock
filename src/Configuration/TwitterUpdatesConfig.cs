namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("twitterUpdates")]
    [JsonObject("twitterUpdates")]
    public class TwitterUpdatesConfig
    {
        [XmlElement("consumerKey")]
        [JsonProperty("consumerKey")]
        public string ConsumerKey { get; set; }

        [XmlElement("consumerSecret")]
        [JsonProperty("consumerSecret")]
        public string ConsumerSecret { get; set; }

        [XmlElement("accessToken")]
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; }

        [XmlElement("accessTokenSecret")]
        [JsonProperty("accessTokenSecret")]
        public string AccessTokenSecret { get; set; }

        [XmlElement("postTwitterUpdates")]
        [JsonProperty("postTwitterUpdates")]
        public bool PostTwitterUpdates { get; set; }

        [XmlElement("users")]
        [JsonProperty("users")]
        public List<ulong> TwitterUsers { get; set; }

        [XmlElement("updatesChannelWebHook")]
        [JsonProperty("updatesChannelWebHook")]
        public string UpdatesChannelWebHook { get; set; }

        public TwitterUpdatesConfig()
        {
            TwitterUsers = new List<ulong>();
        }
    }
}