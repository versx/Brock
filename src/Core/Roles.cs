namespace PokeFilterBot
{
    using System.Collections.Generic;

    using DSharpPlus.Entities;

    public static class Roles
    {
        public static Dictionary<string, DiscordColor> Teams = new Dictionary<string, DiscordColor>
        {
            { "Valor", DiscordColor.Red },
            { "Mystic", DiscordColor.Blue },
            { "Instinct", DiscordColor.Yellow },
        };
    }
}