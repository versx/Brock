namespace BrockBot
{
    using System;
    using System.Collections.Generic;

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandPermissionAttribute : Attribute
    {
        public CommandPermissionLevel PermissionLevel { get; }

        public CommandPermissionAttribute(CommandPermissionLevel permissionLevel)
        {
            PermissionLevel = permissionLevel;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandExampleAttribute : Attribute
    {
        public string Example { get; }

        public CommandExampleAttribute(string example)
        {
            Example = example;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandCategoryAttribute : Attribute
    {
        public string Category { get; }

        public CommandCategoryAttribute(string category)
        {
            Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public CommandDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CommandNamesAttribute : Attribute
    {
        public List<string> Commands { get; }

        public CommandNamesAttribute()
        {
            Commands = new List<string>();
        }

        public CommandNamesAttribute(params string[] commands) : this()
        {
            Commands.AddRange(commands);
        }
    }
}