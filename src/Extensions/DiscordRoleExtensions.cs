namespace BrockBot.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public static class DiscordRoleExtensions
    {
        public static async Task<bool> AssignRole(this DiscordClient client, DiscordMember member, string roleName)
        {
            var role = client.GetRoleFromName(roleName);
            if (role == null)
            {
                Utils.LogError(new Exception($"Failed to find role '{roleName}'."));
                return false;
            }

            if (member.HasRole(role.Id)) return true;

            try
            {
                await member.GrantRoleAsync(role);
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return false;
        }

        public static async Task<bool> AssignRole(this DiscordClient client, DiscordMember member, ulong roleId)
        {
            var role = client.GetRoleFromId(roleId);
            if (role == null)
            {
                Utils.LogError(new Exception($"Failed to find role with id '{roleId}'."));
                return false;
            }

            if (member.HasRole(role.Id)) return true;

            try
            {
                await member.GrantRoleAsync(role);
                return true;
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }

            return false;
        }

        public static DiscordRole GetRoleFromId(this DiscordClient client, ulong roleId)
        {
            foreach (var guild in client.Guilds)
            {
                foreach (var role in guild.Value.Roles)
                {
                    if (role.Id == roleId) return role;
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
    }
}