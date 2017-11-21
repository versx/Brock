namespace BrockBot
{
    using System;
    using System.Collections.Generic;

    public class CommandAttribute : Attribute
    {
        public List<string> CommandNames { get; set; }

        public CommandAttribute()
        {
            CommandNames = new List<string>();
        }

        public CommandAttribute(params string[] args) : this()
        {
            CommandNames.AddRange(args);
        }
    }
}