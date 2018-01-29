#region Old Raid Lobby System
//namespace BrockBot.Commands
//{
//    using System;
//    using System.Threading.Tasks;

//    using DSharpPlus;
//    using DSharpPlus.Entities;

//    using BrockBot.Data;
//    using BrockBot.Extensions;

//    [Command(
//        Categories.RaidLobby,
//        "Lists the user status' in the current raid lobby.",
//        null,
//        "list"
//    )]
//    public class RaidLobbyListUsersCommand : ICustomCommand
//    {
//        #region Properties

//        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

//        public DiscordClient Client { get; }

//        public IDatabase Db { get; }

//        #endregion

//        #region Constructor

//        public RaidLobbyListUsersCommand(DiscordClient client, IDatabase db)
//        {
//            Client = client;
//            Db = db;
//        }

//        #endregion

//        public async Task Execute(DiscordMessage message, Command command)
//        {
//            await message.IsDirectMessageSupported();

//            var lobby = Db.Lobbies.Find(x => x.ChannelId == message.Channel.Id);
//            if (lobby == null)
//            {
//                await message.RespondAsync("Failed to find lobby.");
//                return;
//            }

//            var lobbyUserStatus = await Client.RaidLobbyUserStatus(lobby);
//            await message.RespondAsync(lobbyUserStatus);
//        }
//    }
//}
#endregion