namespace BrockBot
{
    using System;
    using System.Collections.Generic;

    public class CommandAttribute : Attribute
    {
        public string Category { get; }

        public List<string> CommandNames { get; }

        public string Description { get; }

        public string Example { get; }

        public CommandAttribute()
        {
            CommandNames = new List<string>();
        }

        public CommandAttribute(string category, string description, string example, params string[] commands) : this()
        {
            Category = category;
            Description = description;
            Example = example;
            CommandNames.AddRange(commands);
        }
    }
}