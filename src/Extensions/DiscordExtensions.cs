namespace PokeFilterBot.Extensions
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
                Utils.LogError(new Exception("Unrecognized lobby name '{lobbyName}'."));
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

            var lobbyChannel = await client.GetChannelAsync(lobby.ChannelId);
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

            return await pinnedMessage.ModifyAsync(new Optional<string>(CreateLobbyStatus(lobby)));
        }

        public static async Task<DiscordMessage> SendLobbyStatus(this DiscordClient client, RaidLobby lobby, DiscordEmbed embed, bool pin)
        {
            if (lobby == null)
            {
                Utils.LogError(new Exception($"Failed to get lobby from database."));
                return null;
            }

            var lobbyChannel = await client.GetChannelAsync(lobby.ChannelId);
            if (lobbyChannel == null)
            {
                Utils.LogError(new Exception($"Failed to get raid lobby channel from {lobby.LobbyName} ({lobby.ChannelId})."));
                return null;
            }

            var message = await lobbyChannel.SendMessageAsync(CreateLobbyStatus(lobby), false, embed);
            if (pin) await message.PinAsync();
            lobby.PinnedRaidMessageId = message.Id;

            return message;
        }

        private static string CreateLobbyStatus(RaidLobby lobby)
        {
            return $"# **{lobby.LobbyName} RAID LOBBY** ({DateTime.Now.ToLongDateString()})\r\n" +
                   $"**TIME LEFT: {(lobby.ExpireTime - lobby.StartTime).TotalMinutes} Minutes**\r\n" +
                   $"Raid Boss: {lobby.PokemonName}\r\n" +
                   $"Start Time: {lobby.StartTime.ToLongTimeString()}\r\n" +
                   $"Expire Time: {lobby.ExpireTime.ToLongTimeString()}\r\n" +
                   $"Gym Name: {lobby.GymName}\r\n" +
                   $"Address: {lobby.Address}\r\n" +
                   $"No. Users On The Way: {lobby.NumUsersOnTheWay}\r\n" +
                   $"No. Users Checked-In: {lobby.NumUsersCheckedIn}\r\n" +
                   $"**{lobby.NumUsersCheckedIn}/{lobby.NumUsersCheckedIn + lobby.NumUsersOnTheWay}** Users Ready!\r\n";
        }
    }
}