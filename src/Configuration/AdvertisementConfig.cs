namespace BrockBot.Configuration
{
    using System;
    using System.Xml.Serialization;

    using Newtonsoft.Json;

    [XmlRoot("advertisement")]
    [JsonObject("advertisement")]
    public class AdvertisementConfig
    {
        [XmlElement("enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [XmlElement("lastMessageId")]
        [JsonProperty("lastMessageId")]
        public ulong LastMessageId { get; set; }

        [XmlElement("postIntervalMinutes")]
        [JsonProperty("postIntervalMinutes")]
        public int PostInterval { get; set; }

        [XmlElement("message")]
        [JsonProperty("message")]
        public string Message { get; set; }

        [XmlElement("channelId")]
        [JsonProperty("channelId")]
        public ulong ChannelId { get; set; }

        [XmlElement("messageThreshold")]
        [JsonProperty("messageThreshold")]
        public int MessageThreshold { get; set; }

        public AdvertisementConfig()
        {
            MessageThreshold = 10;
        }
    }
}