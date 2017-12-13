namespace BrockBot
{
    using System.Collections.Generic;

    using DSharpPlus.Entities;

    //TODO: Modify config to support dictionary of Team Name and Team Color.
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