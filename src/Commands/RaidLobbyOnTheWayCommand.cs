namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Extensions;

    [Command("ontheway", "otw", "onmyway", "omw")]
    public class RaidLobbyOnTheWayCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public RaidLobbyOnTheWayCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

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

            var numPeople = 1;
            if (!string.IsNullOrEmpty(amountOfPeople))
            {
                if (!int.TryParse(amountOfPeople, out int value))
                {
                    await message.RespondAsync("You entered an invalid value for the amount of people that are on the way. Please make sure it is a numerical value or if it's just yourself you do not need to specify the amount of people.");
                    return;
                }

                numPeople = value;
            }

            if (message.Channel == null) return;
            var server = Db[message.Channel.GuildId];
            if (server == null) return;

            var lobby = server.Lobbies.Find(x => string.Compare(x.LobbyName, lobbyName, true) == 0);
            if (lobby == null)
            {
                await message.RespondAsync($"Lobby {lobbyName} does not exist.");
                return;
            }

            var lobbyChannel = await Client.GetChannel(lobby.ChannelId);
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
                numPeople,
                eta
            )
            { OnTheWayTime = DateTime.Now });

            await message.RespondAsync($"{message.Author.Mention} is on the way to raid lobby {lobbyChannel.Name} with {lobby.UserCheckInList[message.Author.Id].UserCount} people and an ETA of {eta}.");
            await lobbyChannel.SendMessageAsync($"{message.Author.Mention} is on the way to raid lobby **{lobbyChannel.Name}** with **{lobby.UserCheckInList[message.Author.Id].UserCount}** people and an **ETA of {eta}**.");

            await Client.UpdateLobbyStatus(lobby);
        }
    }
}