﻿namespace PokeFilterBot.Extensions
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data.Models;
    using PokeFilterBot.Utilities;

    public static class DiscordExtensions
    {
        public static DiscordChannel GetChannelByName(this DiscordClient client, string channelName)
        {
            foreach (var guild in client.Guilds)
            {
                foreach (var channel in guild.Value.Channels)
                {
                    if (string.Compare(channel.Name, channelName, true) == 0)
                    {
                        return channel;
                    }
                }
            }

            return null;
        }

        public static async Task<DiscordChannel> GetChannel(this DiscordClient client, ulong channelId)
        {
            try
            {
                return await client.GetChannelAsync(channelId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static DiscordRole GetRoleFromName(this DiscordClient client, string roleName)
        {
            foreach (var guild in client.Guilds)
            {
                foreach (var role in guild.Value.Roles)
                {
                    if (string.Compare(role.Name, roleName, true) == 0)
                    {
                        return role;
                    }
                }
            }

            return null;
        }

        public static async Task<DiscordMember> GetMemberFromUserId(this DiscordClient client, ulong userId)
        {
            foreach (var guild in client.Guilds)
            {
                var user = await guild.Value.GetMemberAsync(userId);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        public static async Task<DiscordMessage> GetMessageById(this DiscordClient client, ulong guildId, ulong messageId)
        {
            var guild = await client.GetGuildAsync(guildId);
            if (guild == null) return null;

            foreach (var channel in guild.Channels)
            {
                try
                {
                    var message = await channel.GetMessageAsync(messageId);
                    if (message != null)
                    {
                        return message;
                    }
                }
#pragma warning disable RECS0022
                catch { }
#pragma warning restore RECS0022
            }

            return null;
        }

        public static async Task<DiscordMessage> GetFirstMessage(this DiscordClient client, string lobbyName)
        {
            var lobbyChannel = client.GetChannelByName(lobbyName);
            if (lobbyChannel == null)
            {
                Utils.LogError(new Exception($"Unrecognized lobby name '{lobbyName}'."));
                return null;
            }

            var messages = await lobbyChannel.GetMessagesAsync();
            if (messages != null)
            {
                return messages[messages.Count - 1];
            }

            return null;
        }

        public static async Task<DiscordMessage> UpdateLobbyStatus(this DiscordClient client, RaidLobby lobby)
        {
            if (lobby == null)
            {
                Utils.LogError(new Exception($"Failed to get lobby from database."));
                return null;
            }

            var lobbyChannel = await client.GetChannel(lobby.ChannelId);
            if (lobbyChannel == null)
            {
                Utils.LogError(new Exception($"Failed to get raid lobby channel from {lobby.LobbyName} ({lobby.ChannelId})."));
                return null;
            }

            var pinnedMessage = await lobbyChannel.GetMessageAsync(lobby.PinnedRaidMessageId);
            if (pinnedMessage == null)
            {
                Utils.LogError(new Exception($"Failed to get pinned raid lobby message from message id {lobby.PinnedRaidMessageId}."));
                return null;
            }

            return await pinnedMessage.ModifyAsync(new Optional<string>(await CreateLobbyStatus(client, lobby)));
        }

        public static async Task<DiscordMessage> SendLobbyStatus(this DiscordClient client, RaidLobby lobby, DiscordEmbed embed, bool pin)
        {
            if (lobby == null)
            {
                Utils.LogError(new Exception($"Failed to get lobby from database."));
                return null;
            }

            var lobbyChannel = await client.GetChannel(lobby.ChannelId);
            if (lobbyChannel == null)
            {
                Utils.LogError(new Exception($"Failed to get raid lobby channel from {lobby.LobbyName} ({lobby.ChannelId})."));
                return null;
            }

            var message = await lobbyChannel.SendMessageAsync(await CreateLobbyStatus(client, lobby), false, embed);
            if (pin) await message.PinAsync();
            lobby.PinnedRaidMessageId = message.Id;

            return message;
        }

        private static async Task<string> CreateLobbyStatus(DiscordClient client, RaidLobby lobby)
        {
            return $"**{lobby.LobbyName} RAID LOBBY** ({DateTime.Now.ToLongDateString()})\r\n" +
                   $"**{(uint)(lobby.ExpireTime - DateTime.Now).TotalMinutes} Minutes Left!**\r\n" +
                   $"Raid Boss: **{lobby.PokemonName}**\r\n" +
                   $"Start Time: {lobby.StartTime.ToLongTimeString()}\r\n" +
                   $"Expire Time: {lobby.ExpireTime.ToLongTimeString()}\r\n" +
                   $"Gym Name: {lobby.GymName}\r\n" +
                   $"Address: {lobby.Address}\r\n\r\n" +
                   await RaidLobbyUserStatus(client, lobby);
        }

        public static async Task<string> RaidLobbyUserStatus(this DiscordClient client, RaidLobby lobby)
        {
            var lobbyUserStatus = "**Raid Lobby User Status:**\r\n";

            foreach (var lobbyUser in lobby.UserCheckInList)
            {
                var user = await client.GetUserAsync(lobbyUser.UserId);
                if (user == null)
                {
                    Utils.LogError(new Exception($"Failed to find user {lobbyUser.UserId}"));
                    return string.Empty;
                }

                var people = lobbyUser.UserCount;

                if (lobbyUser.IsCheckedIn && !lobbyUser.IsOnTheWay)
                {
                    lobbyUserStatus += $"{user.Username} **checked-in** at {lobbyUser.CheckInTime.ToLongTimeString()} and is ready to start.\r\n";
                }
                else
                {
                    lobbyUserStatus += $"{user.Username} was **on the way** at **{lobbyUser.OnTheWayTime.ToLongTimeString()}** with {lobbyUser.UserCount} participants and an ETA of {lobbyUser.ETA}.\r\n";
                }

                lobbyUserStatus += 
                    $"{lobby.NumUsersOnTheWay} users on their way.\r\n" +
                    $"{lobby.NumUsersCheckedIn} users already checked in and ready.\r\n" +
                    $"**{lobby.NumUsersCheckedIn}/{lobby.NumUsersCheckedIn + lobby.NumUsersOnTheWay}** Users Ready!\r\n";
            }

            return lobbyUserStatus;
        }
    }
}