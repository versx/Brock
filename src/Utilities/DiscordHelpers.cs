namespace BrockBot.Utilities
{
    using DSharpPlus.Entities;

    public static class DiscordHelpers
    {
        public static DiscordColor BuildColor(string iv)
        {
            if (int.TryParse(iv.Substring(0, iv.Length - 1), out int result))
            {
                if (result == 100)
                    return DiscordColor.Green;
                else if (result >= 90 && result < 100)
                    return DiscordColor.Orange;
                else if (result < 90)
                    return DiscordColor.Yellow;
            }

            return DiscordColor.White;
        }

        public static DiscordColor BuildRaidColor(int level)
        {
            switch (level)
            {
                case 1:
                    return DiscordColor.HotPink;
                case 2:
                    return DiscordColor.HotPink;
                case 3:
                    return DiscordColor.Yellow;
                case 4:
                    return DiscordColor.Yellow;
                case 5:
                    return DiscordColor.Purple;
            }

            return DiscordColor.White;
        }

        public static ulong ConvertMentionToUserId(string mention)
        {
            //<@201909896357216256>
            mention = Utils.GetBetween(mention, "<", ">");
            mention = mention.Replace("@", null);
            mention = mention.Replace("!", null);

            if (ulong.TryParse(mention, out ulong result))
            {
                return result;
            }

            return 0;
        }
    }
}