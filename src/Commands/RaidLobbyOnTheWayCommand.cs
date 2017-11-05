namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;
    using PokeFilterBot.Extensions;

    public class RaidLobbyOnTheWayCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public RaidLobbyOnTheWayCommand(DiscordClient client, Database db)
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
                if (string.IsNullOrEmpty(lobbyName))
                {
                    await message.RespondAsync("You must enter a lobby name in order to send the on the way command.");
                    return;
                }

                var eta = command.Args[1];
                var lobbyChannel = _client.GetChannelByName(lobbyName);
                if (lobbyChannel == null)
                {
                    await message.RespondAsync("Unrecognized lobby name.");
                    return;
                }

                await lobbyChannel.SendMessageAsync($"{message.Author.Username} is on the way to raid lobby {lobbyChannel.Name} with an ETA of {eta}.");

                //TODO: Edit pinned message or add to channel that x amount of people are on the way and x amount are checked in waiting.
            }
        }
    }
}