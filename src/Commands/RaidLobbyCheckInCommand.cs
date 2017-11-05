namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;
    using PokeFilterBot.Extensions;

    public class RaidLobbyCheckInCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public RaidLobbyCheckInCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            if (command.Args.Count == 2)
            {
                var lobbyName = command.Args[0];
                var lobby = _db.Lobbies.Find(x => x.LobbyName == lobbyName);
                if (lobby != null)
                {
                    if (!lobby.PlayersCheckedIn.Contains(message.Author.Id))
                    {
                        lobby.PlayersCheckedIn.Add(message.Author.Id);
                    }
                }
                var count = command.Args[1];
                var lobbyChannel = _client.GetChannelByName(lobbyName);
                if (lobbyChannel == null)
                {
                    await message.RespondAsync("Unrecognized lobby name.");
                    return;
                }
                await lobbyChannel.SendMessageAsync($"{message.Author.Username} has checked into raid lobby '{lobbyChannel.Name}' with {count} people.");
            }
        }
    }
}