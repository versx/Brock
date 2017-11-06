namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;
    using PokeFilterBot.Data.Models;
    using PokeFilterBot.Extensions;
    using PokeFilterBot.Utilities;

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

            switch (command.Args.Count)
            {
                case 2:
                    await SetRaidLobbyUserOnTheWay(message, command.Args[0], command.Args[1], string.Empty);
                    break;
                case 3:
                    await SetRaidLobbyUserOnTheWay(message, command.Args[0], command.Args[1], command.Args[2]);
                    break;
            }
        }

        private async Task SetRaidLobbyUserOnTheWay(DiscordMessage message, string lobbyName, string eta, string amountOfPeople)
        {
            if (string.IsNullOrEmpty(lobbyName))
            {
                await message.RespondAsync("You must enter a lobby name in order to send the on the way command.");
                return;
            }

            if (string.IsNullOrEmpty(eta))
            {
                await message.RespondAsync("You must enter the ETA to the raid lobby you're interested in.");
                return;
            }

            //if (string.IsNullOrEmpty(amountOfPeople))
            //{
            //    await message.RespondAsync("You must enter the amount of people you are with including yourself.");
            //    return;
            //}

            var lobby = _db.Lobbies.Find(x => string.Compare(x.LobbyName, lobbyName, true) == 0);
            if (lobby == null)
            {
                await message.RespondAsync($"Lobby {lobbyName} does not exist.");
                return;
            }

            var lobbyChannel = await _client.GetChannelAsync(lobby.ChannelId);
            if (lobbyChannel == null)
            {
                await message.RespondAsync("Unrecognized lobby name.");
                return;
            }

            //Checks if the user is in the check-in list as well as if they are already on the way but not checked in yet.
            if (lobby.UserCheckInList.ContainsKey(message.Author.Id) &&
                lobby.UserCheckInList[message.Author.Id].IsOnTheWay && 
                !lobby.UserCheckInList[message.Author.Id].IsCheckedIn)
            {
                await message.RespondAsync($"You are already set as on the way for lobby {lobbyName} with {lobby.UserCheckInList[message.Author.Id].UserCount} people and an ETA of {lobby.UserCheckInList[message.Author.Id].ETA}.");
                return;
            }

            lobby.UserCheckInList.Add(new RaidLobbyUser
            (
                message.Author.Id,
                false,
                true,
                string.IsNullOrEmpty(amountOfPeople) ? 1 : Convert.ToInt32(amountOfPeople),
                eta
            )
            { OnTheWayTime = DateTime.Now });

            await message.RespondAsync($"{message.Author.Mention} is on the way to raid lobby {lobbyChannel.Name} with {lobby.UserCheckInList[message.Author.Id].UserCount} people and an ETA of {eta}.");
            await lobbyChannel.SendMessageAsync($"{message.Author.Mention} is on the way to raid lobby **{lobbyChannel.Name}** with **{lobby.UserCheckInList[message.Author.Id].UserCount}** people and an **ETA of {eta}**.");

            await _client.UpdateLobbyStatus(lobby);
        }
    }
}