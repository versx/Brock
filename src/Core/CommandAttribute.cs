namespace BrockBot
{
    using System;
    using System.Collections.Generic;

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Category { get; }

        public List<string> CommandNames { get; }

        public string Description { get; }

        public string Example { get; }

        public CommandPermissionLevel PermissionLevel { get; }

        public CommandAttribute()
        {
            CommandNames = new List<string>();
        }

        public CommandAttribute(string category, string description, string example, /*CommandPermissionLevel permissionLevel,*/ params string[] commands) : this()
        {
            Category = category;
            Description = description;
            Example = example;
            //PermissionLevel = permissionLevel;
            CommandNames.AddRange(commands);
        }
    }
}