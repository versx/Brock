namespace BrockBot.Extensions
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public static class DiscordExtensions
    {
        #region Old Raid Lobby Extensions

        //public static async Task<DiscordMessage> GetFirstMessage(this DiscordClient client, string lobbyName)
        //{
        //    var lobbyChannel = client.GetChannelByName(lobbyName);
        //    if (lobbyChannel == null)
        //    {
        //        Utils.LogError(new Exception($"Unrecognized lobby name '{lobbyName}'."));
        //        return null;
        //    }

        //    var messages = await lobbyChannel.GetMessagesAsync();
        //    if (messages != null)
        //    {
        //        return messages[messages.Count - 1];
        //    }

        //    return null;
        //}

        #region Old Raid Lobby System
        //public static async Task<DiscordMessage> UpdateLobbyStatus(this DiscordClient client, RaidLobby lobby)
        //{
        //    if (lobby == null)
        //    {
        //        Utils.LogError(new Exception($"Failed to get lobby from database."));
        //        return null;
        //    }

        //    var lobbyChannel = await client.GetChannel(lobby.ChannelId);
        //    if (lobbyChannel == null)
        //    {
        //        Utils.LogError(new Exception($"Failed to get raid lobby channel from {lobby.LobbyName} ({lobby.ChannelId})."));
        //        return null;
        //    }

        //    var pinnedMessage = await lobbyChannel.GetMessageAsync(lobby.PinnedRaidMessageId);
        //    if (pinnedMessage == null)
        //    {
        //        Utils.LogError(new Exception($"Failed to get pinned raid lobby message from message id {lobby.PinnedRaidMessageId}."));
        //        return null;
        //    }

        //    return await pinnedMessage.ModifyAsync(new Optional<string>(await CreateLobbyStatus(client, lobby)));
        //}

        //public static async Task<DiscordMessage> SendLobbyStatus(this DiscordClient client, RaidLobby lobby, DiscordEmbed embed, bool pin)
        //{
        //    if (lobby == null)
        //    {
        //        Utils.LogError(new Exception($"Failed to get lobby from database."));
        //        return null;
        //    }

        //    var lobbyChannel = await client.GetChannel(lobby.ChannelId);
        //    if (lobbyChannel == null)
        //    {
        //        Utils.LogError(new Exception($"Failed to get raid lobby channel from {lobby.LobbyName} ({lobby.ChannelId})."));
        //        return null;
        //    }

        //    var message = await lobbyChannel.SendMessageAsync(await CreateLobbyStatus(client, lobby), false, embed);
        //    if (pin) await message.PinAsync();
        //    lobby.PinnedRaidMessageId = message.Id;

        //    return message;
        //}

        //public static async Task<string> RaidLobbyUserStatus(this DiscordClient client, RaidLobby lobby)
        //{
        //    var lobbyUserStatus = "**Raid Lobby User Status:**\r\n";

        //    foreach (var lobbyUser in lobby.UserCheckInList)
        //    {
        //        var user = await client.GetUser(lobbyUser.UserId);
        //        if (user == null)
        //        {
        //            Utils.LogError(new Exception($"Failed to find user {lobbyUser.UserId}"));
        //            return string.Empty;
        //        }

        //        var people = lobbyUser.UserCount;

        //        if (lobbyUser.IsCheckedIn && !lobbyUser.IsOnTheWay)
        //        {
        //            lobbyUserStatus += $"{user.Mention} **checked-in** at {lobbyUser.CheckInTime.ToLongTimeString()} and is ready to start.\r\n";
        //        }
        //        else
        //        {
        //            lobbyUserStatus += $"{user.Mention} was **on the way** at **{lobbyUser.OnTheWayTime.ToLongTimeString()}** with {lobbyUser.UserCount} participants and an ETA of {lobbyUser.ETA}.\r\n";
        //        }

        //        lobbyUserStatus += 
        //            $"{lobby.NumUsersOnTheWay} users on their way.\r\n" +
        //            $"{lobby.NumUsersCheckedIn} users already checked in and ready.\r\n" +
        //            $"**{lobby.NumUsersCheckedIn}/{lobby.NumUsersCheckedIn + lobby.NumUsersOnTheWay}** Users Ready!\r\n";
        //    }

        //    return lobbyUserStatus;
        //}

        //private static async Task<string> CreateLobbyStatus(DiscordClient client, RaidLobby lobby)
        //{
        //    return $"**{lobby.LobbyName} RAID LOBBY** ({DateTime.Now.ToLongDateString()})\r\n" +
        //           $"**{Convert.ToUInt32(lobby.MinutesLeft)} Minutes Left!**\r\n" + //TODO: Fix minutes left.
        //           $"Raid Boss: **{lobby.PokemonName}**\r\n" +
        //           $"Start Time: {lobby.StartTime.ToLongTimeString()}\r\n" +
        //           $"Expire Time: {lobby.ExpireTime.ToLongTimeString()}\r\n" +
        //           $"Gym Name: {lobby.GymName}\r\n" +
        //           $"Address: {lobby.Address}\r\n\r\n" +
        //           await RaidLobbyUserStatus(client, lobby);
        //}
        #endregion

        #endregion

        #region Raid Lobby Reaction Extensions

        public static async Task SetDefaultRaidReactions(this DiscordClient client, DiscordMessage message, bool isLobby, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                await message.DeleteAllReactionsAsync();
                await Utils.Wait(10);
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrow_right:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":white_check_mark:"));
            if (isLobby)
            {
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":x:"));
                await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":arrows_counterclockwise:"));
            }
        }

        public static async Task SetAccountsReactions(this DiscordClient client, DiscordMessage message, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                await message.DeleteAllReactionsAsync();
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":one:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":two:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":three:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":four:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":five:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":six:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":seven:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":eight:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":nine:"));
            //await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":ten:"));
        }

        public static async Task SetEtaReactions(this DiscordClient client, DiscordMessage message, bool deleteExisting = true)
        {
            if (client == null) return;

            if (deleteExisting)
            {
                await message.DeleteAllReactionsAsync();
            }

            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":five:"));
            await message.CreateReactionAsync(DiscordEmoji.FromName(client, ":keycap_ten:"));
            //TODO: Add more ETA reactions.
        }

        #endregion
    }
}