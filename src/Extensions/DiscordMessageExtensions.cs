namespace BrockBot.Extensions
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public static class DiscordMessageExtensions
    {
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
    }
}