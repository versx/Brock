namespace BrockBot
{
    using System;
    using System.Collections.Generic;

    using BrockBot.Commands;

    public class CommandList : Dictionary<string[], ICustomCommand>
    {
        public ICustomCommand this[string key]
        {
            get
            {
                foreach (var command in this)
                {
                    foreach (var cmdName in command.Key)
                    {
                        if (string.Compare(cmdName, key, true) == 0)
                        {
                            return command.Value;
                        }
                    }
                }

                return null;
            }
        }

        public bool ContainsKey(string key)
        {
            foreach (var command in this)
            {
                foreach (var alias in command.Key)
                {
                    if (string.Compare(alias, key, true) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}