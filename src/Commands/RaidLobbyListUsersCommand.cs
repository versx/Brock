namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    public class RaidLobbyListUsersCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public RaidLobbyListUsersCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            var lobby = server.Lobbies.Find(x => x.ChannelId == message.Channel.Id);
            if (lobby == null)
            {
                await message.RespondAsync("Failed to find lobby.");
                return;
            }

            var lobbyUserStatus = await _client.RaidLobbyUserStatus(lobby);
            await message.RespondAsync(lobbyUserStatus);
        }
    }
}