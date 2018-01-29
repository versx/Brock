namespace BrockBot.Configuration
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using BrockBot.Data.Models;

    [JsonObject("raidLobbies")]
    public class RaidLobbyConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("raidLobbiesChannelId")]
        public ulong RaidLobbiesChannelId { get; set; }

        [JsonProperty("lobbies")]
        public Dictionary<ulong, RaidLobby> ActiveLobbies { get; set; }

        public RaidLobbyConfig()
        {
            ActiveLobbies = new Dictionary<ulong, RaidLobby>();
        }
    }
}