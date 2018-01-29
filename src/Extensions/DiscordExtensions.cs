namespace BrockBot.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data.Models;
    using BrockBot.Utilities;

    public static class DiscordExtensions
    {
        #region Channel Extensions

        public static async Task IsDirectMessageSupported(this DiscordMessage message)
        {
            if (message.Channel.Guild == null)
            {
                await message.RespondAsync("DM is not supported for this command yet.");
                return;
            }
        }

        public static DiscordChannel GetChannelByName(this DiscordClient client, string channelName, bool isTextChannel = true)
        {
            foreach (var guild in client.Guilds)
            {
                foreach (var channel in guild.Value.Channels)
                {
                    if (string.Compare(channel.Name, channelName, true) == 0 && (channel.IsCategory && !isTextChannel || !channel.IsCategory && isTextChannel))
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

        #endregion

        #region Role Extensions

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

        public static async Task GrantPermissions(this DiscordChannel channel, DiscordRole role, Permissions allow, Permissions deny)
        {
            if (role.CheckPermission(allow) != PermissionLevel.Allowed)
            {
                await channel.AddOverwriteAsync(role, allow, deny, $"Setting @{role.Name} role permissions for channel #team_{role.Name}.");
            }
        }

        public static async Task<bool> RemoveRole(this DiscordClient client, ulong userId, ulong guildId, ulong roleId)
        {
            try
            {
                var member = await client.GetMemberFromUserId(userId);
                if (member == null)
                {
                    Utils.LogError(new Exception($"Failed to find member with id {userId}."));
                    return false;
                }

                var guild = await client.GetGuildAsync(guildId);
                if (guild == null)
                {
                    Utils.LogError(new Exception($"Failed to find guild with id {guildId}."));
                    return false;
                }

                var role = guild.GetRole(roleId);
                if (role == null)
                {
                    Utils.LogError(new Exception($"Failed to find role with id {roleId}."));
                    return false;
                }

                await guild.RevokeRoleAsync(member, role, "Supporter status expired.");

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region User Extensions

        public static async Task<DiscordUser> GetUser(this DiscordClient client, ulong userId)
        {
            try
            {
                return await client.GetUserAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<DiscordMember> GetMemberFromUserId(this DiscordClient client, ulong userId)
        {
            try
            {
                foreach (var guild in client.Guilds)
                {
                    var user = await guild.Value.GetMemberAsync(userId);
                    if (user != null)
                    {
                        return user;
                    }
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return null;
        }

        public static void AssignMemberRoles(this DiscordClient client, DiscordMember member, List<string> roles)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x =>
#pragma warning restore RECS0165
            {
                foreach (var city in roles)
                {
                    var cityRole = client.GetRoleFromName(city);
                    if (cityRole == null)
                    {
                        //Failed to find role.
                        Utils.LogError(new Exception($"Failed to find city role {city}, please make sure it exists."));
                        continue;
                    }

                    await member.GrantRoleAsync(cityRole, "Default city role assignment initialization.");
                }
            })
            { IsBackground = true }.Start();
        }

        public static async Task<bool> HasSupporterRole(this DiscordClient client, ulong userId, ulong supporterRoleId)
        {
            var member = await client.GetMemberFromUserId(userId);
            if (member == null)
            {
                Console.WriteLine($"Failed to get user with id {userId}.");
                return false;
            }

            return member.HasSupporterRole(supporterRoleId);
        }

        public static bool HasSupporterRole(this DiscordMember member, ulong supporterRoleId)
        {
            return HasRole(member, supporterRoleId);
        }

        public static async Task<bool> HasModeratorRole(this DiscordClient client, ulong userId, ulong moderatorRoleId)
        {
            var member = await client.GetMemberFromUserId(userId);
            if (member == null)
            {
                Console.WriteLine($"Failed to get user with id {userId}.");
                return false;
            }

            return member.HasModeratorRole(moderatorRoleId);
        }

        public static bool HasModeratorRole(this DiscordMember member, ulong moderatorRoleId)
        {
            return HasRole(member, moderatorRoleId);
        }

        public static bool HasRole(this DiscordMember member, ulong roleId)
        {
            foreach (var role in member.Roles)
            {
                if (role.Id == roleId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasRole(this DiscordClient client, DiscordMember member, string roleName)
        {
            var role = client.GetRoleFromName(roleName);
            if (role == null) return false;

            return HasRole(member, role.Id);
        }

        public static async Task<bool> IsSupporterStatusExpired(this DiscordClient client, Configuration.Config config, ulong userId)
        {
            var user = await client.GetUser(userId);
            if (user == null)
            {
                Utils.LogError(new Exception($"User with id {userId} does not exist..."));
                return true;
            }

            if (!config.Supporters.ContainsKey(userId))
            {
                Utils.LogError(new Exception($"User with id {userId} does not exist in the supporters list..."));
                return true;
            }

            var supporter = config.Supporters[userId];
            var diff = DateTime.Now.Subtract(supporter.DateDonated);

            return diff.Days > 0 || diff.Hours > 0;
        }

        #endregion

        #region Raid Lobby Extensions

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

        #region Message Extensions

        public static async Task<DiscordMessage> GetMessage(this DiscordChannel channel, ulong messageId)
        {
            try
            {
                return await channel.GetMessageAsync(messageId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task RespondAsync(this DiscordMessage message, string content, DiscordEmbed embed = null, int deleteAfterMs = 10 * 1000)
        {
            var msg = await message.RespondAsync(content, false, embed);

            if (deleteAfterMs > 0)
            {
                await Utils.Wait(deleteAfterMs);
                await msg.DeleteAsync();
            }
        }

        public static async Task<DiscordMessage> GetMessageById(this DiscordClient client, ulong guildId, ulong messageId)
        {
            var guild = await client.GetGuildAsync(guildId);
            if (guild == null) return null;

            foreach (var channel in guild.Channels)
            {
                var message = await channel.GetMessage(messageId);
                if (message != null)
                {
                    return message;
                }
            }

            return null;
        }

        public static async Task SendMessage(this DiscordClient client, string webHookUrl, string message, DiscordEmbed embed = null)
        {
            var data = Utils.GetWebHookData(webHookUrl);
            if (data == null) return;

            if (!ulong.TryParse(Convert.ToString(data["guild_id"]), out ulong guildId))
            {
                Console.WriteLine("Error: Failed to parse guild_id from webhook data.");
                return;
            }

            if (!ulong.TryParse(Convert.ToString(data["channel_id"]), out ulong channelId))
            {
                Console.WriteLine("Error: Failed to parse channel_id from webhook data.");
                return;
            }

            var guild = await client.GetGuildAsync(guildId);
            if (guild == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Guild does not exist!");
                Console.ResetColor();
                return;
            }
            var channel = await client.GetChannel(channelId);
            //var channel = await client.GetChannelAsync(channelId);
            if (channel == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Channel does not exist!");
                Console.ResetColor();
                return;
            }

            await channel.SendMessageAsync(message, false, embed);
        }

        public static async Task<DiscordMessage> SendDirectMessage(this DiscordClient client, DiscordUser user, string message, DiscordEmbed embed)
        {
            if (string.IsNullOrEmpty(message) && embed == null) return null;

            try
            {
                var dm = await client.CreateDmAsync(user);
                if (dm != null)
                {
                    var msg = await dm.SendMessageAsync(message, false, embed);
                    return msg;
                }
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return null;
        }

        public static async Task SendWelcomeMessage(this DiscordClient client, DiscordUser user, string welcomeMessage)
        {
            try
            {
                await client.SendDirectMessage
                (
                    user,
                    ReplaceInfo(welcomeMessage, user),
                    null
                );
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }

        private static string ReplaceInfo(string message, DiscordUser user)
        {
            if (message == null) return null;

            return message
                .Replace("{username}", user.Username)
                .Replace("{mention}", user.Mention);
        }

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

        public static void LogUnauthorizedAccess(this DiscordUser user, string filePath)
        {
            try
            {
                File.AppendAllText(filePath, $"{DateTime.Now} >> {user.Username}:{user.Id}\r\n");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }
    }
}