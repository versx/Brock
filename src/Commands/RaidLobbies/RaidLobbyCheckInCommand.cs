#region Old Raid Lobby System
//namespace BrockBot.Commands
//{
//    using System;
//    using System.Threading.Tasks;

//    using DSharpPlus;
//    using DSharpPlus.Entities;

//    using BrockBot.Data;
//    using BrockBot.Data.Models;
//    using BrockBot.Extensions;

//    [Command(
//        Categories.RaidLobby,
//        "Checks you into the specified raid lobby informing you are now at the raid.",
//        "\tExample: `.here Magikarp_4th`\r\n" +
//        "\tExample: `.checkin ttar_test`",
//        "checkin", "here"
//    )]
//    public class RaidLobbyCheckInCommand : ICustomCommand
//    {
//        #region Properties

//        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

//        public DiscordClient Client { get; }

//        public IDatabase Db { get; }

//        #endregion

//        #region Constructor

//        public RaidLobbyCheckInCommand(DiscordClient client, IDatabase db)
//        {
//            Client = client;
//            Db = db;
//        }

//        #endregion

//        public async Task Execute(DiscordMessage message, Command command)
//        {
//            if (!command.HasArgs) return;
//            if (command.Args.Count != 1) return;


//            await message.IsDirectMessageSupported();

//            var lobbyName = command.Args[0];
//            if (string.IsNullOrEmpty(lobbyName))
//            {
//                await message.RespondAsync("You must enter a lobby name in order to send the on the way command.");
//                return;
//            }

//            var lobby = Db.Lobbies.Find(x => string.Compare(x.LobbyName, lobbyName, true) == 0);
//            if (lobby == null)
//            {
//                await message.RespondAsync($"Lobby {lobbyName} does not exist.");
//                return;
//            }

//            var lobbyChannel = await Client.GetChannel(lobby.ChannelId);
//            if (lobbyChannel == null)
//            {
//                await message.RespondAsync("Unrecognized lobby name.");
//                return;
//            }

//            if (lobby.UserCheckInList.ContainsKey(message.Author.Id))
//            {
//                if (lobby.UserCheckInList[message.Author.Id].IsCheckedIn)
//                {
//                    await message.RespondAsync($"You are already checked-in to raid lobby {lobbyName} with {lobby.UserCheckInList[message.Author.Id].UserCount} people with you.");
//                    return;
//                }

//                lobby.UserCheckInList[message.Author.Id].CheckInTime = DateTime.Now;
//                lobby.UserCheckInList[message.Author.Id].IsCheckedIn = true;
//                lobby.UserCheckInList[message.Author.Id].IsOnTheWay = false;
//            }
//            else
//            {
//                lobby.UserCheckInList.Add(new RaidLobbyUser(message.Author.Id, true, false, 1, string.Empty));
//            }

//            await message.RespondAsync($"{message.Author.Mention} has checked into raid lobby {lobbyChannel.Name} as ready with {lobby.UserCheckInList[message.Author.Id].UserCount} people.");
//            await lobbyChannel.SendMessageAsync($"{message.Author.Mention} has checked into raid lobby **{lobbyChannel.Name}** as ready with **{lobby.UserCheckInList[message.Author.Id].UserCount}** people.");

//            await Client.UpdateLobbyStatus(lobby);
//        }
//    }
//}
////Have a global list of people and the amount of time they have waited since they checked in.
///**
// * Users can checkin without setting on the way.
// */
#endregion