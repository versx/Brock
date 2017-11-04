namespace PokeFilterBot.Extensions
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    public static class DiscordExtensions
    {
        public static DiscordChannel GetChannelByName(this DiscordClient client, string channelName)
        {
            foreach (var guild in client.Guilds)
            {
                foreach (var channel in guild.Value.Channels)
                {
                    if (channel.Name == channelName)
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
    }
}