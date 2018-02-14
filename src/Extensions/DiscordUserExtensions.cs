namespace BrockBot.Extensions
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Utilities;

    public static class DiscordUserExtensions
    {
        public static void LogUnauthorizedAccess(this DiscordUser user, string command, string filePath)
        {
            try
            {
                File.AppendAllText(filePath, $"{DateTime.Now} >> {user.Username}:{user.Id} - {command}\r\n");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }

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

        public static async Task<bool> IsSupporterOrHigher(this DiscordClient client, ulong userId, Config config)
        {
            try
            {
                var isAdmin = userId == config.OwnerId;
                if (isAdmin) return true;

                var isModerator = config.Moderators.Contains(userId);
                if (isModerator) return true;

                var isSupporter = await client.HasSupporterRole(userId, config.SupporterRoleId);
                if (isSupporter) return true;

                var isElite = await client.HasSupporterRole(userId, config.TeamEliteRoleId);
                if (isElite) return true;
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return false;
        }

        public static bool IsModeratorOrHigher(this ulong userId, Config config)
        {
            var isAdmin = userId == config.OwnerId;
            if (isAdmin) return true;

            var isModerator = config.Moderators.Contains(userId);
            if (isModerator) return true;

            return false;
        }

        public static bool IsModerator(this ulong userId, Config config)
        {
            return config.Moderators.Contains(userId);
        }

        public static bool IsAdmin(this ulong userId, Config config)
        {
            return userId == config.OwnerId;
        }

        public static async Task<bool> IsSupporterStatusExpired(this DiscordClient client, Config config, ulong userId)
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
    }
}