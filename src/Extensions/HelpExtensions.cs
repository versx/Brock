namespace BrockBot.Extensions
{
    using System;
    using System.Collections.Generic;

    using BrockBot.Commands;

    public static class HelpExtensions
    {
        public static Dictionary<string, List<ICustomCommand>> GetCommandsByCategory(this CommandList commands)
        {
            var categories = new Dictionary<string, List<ICustomCommand>>();
            foreach (var cmd in commands)
            {
                var attr = cmd.Value.GetType().GetAttribute<CommandAttribute>();
                if (!categories.ContainsKey(attr.Category))
                {
                    categories.Add(attr.Category, new List<ICustomCommand>());
                }
                categories[attr.Category].Add(cmd.Value);
            }
            return categories;
        }

        public static string ParseCategory(this CommandList commands, string shorthandCategory)
        {
            var helpCategory = shorthandCategory.ToLower();
            foreach (var key in GetCommandsByCategory(commands))
            {
                if (key.Key.ToLower().Replace(" ", "") == helpCategory)
                {
                    helpCategory = key.Key;
                }
            }
            return helpCategory;
        }
    }
}