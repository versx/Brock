namespace BrockBot.Services.RaidLobby
{
    using DSharpPlus.Entities;

    using BrockBot.Data.Models;

    public class RaidLobbySettings
    {
        public RaidLobby Lobby { get; set; }

        public DiscordChannel OriginalRaidMessageChannel { get; set; }

        public DiscordMessage RaidMessage { get; set; }

        public DiscordChannel RaidLobbyChannel { get; set; }
    }
}