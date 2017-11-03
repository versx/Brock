namespace PokeFilterBot
{
	using System;
	using System.Collections.Generic;

	public class Command
	{
		public const char Prefix = '.';

		#region Properties

        /// <summary>
        /// Gets the full command line received.
        /// </summary>
		public string FullCommand { get; private set; }

        /// <summary>
        /// Gets the name of the command. e.g. help
        /// </summary>
		public string Name { get; private set; }

        /// <summary>
        /// Gets a value determining whether the command contains arguments.
        /// </summary>
		public bool HasArgs { get; private set; }

        /// <summary>
        /// Gets a value determining whether the command is valid.
        /// </summary>
		public bool ValidCommand { get; private set; }

        /// <summary>
        /// Gets a list of the arguments passed.
        /// </summary>
		public List<string> Args { get; private set; }

		#endregion

		#region Constructor

		public Command(string command)
		{
            ParseCommand(command);
		}

		#endregion

        private void ParseCommand(string command)
        {
            FullCommand = command;
            ValidCommand = (FullCommand[0] == Prefix);

            if (ValidCommand)
            {
                HasArgs = FullCommand.Contains(" ");

                if (HasArgs)
                {
                    Args = new List<string>();
                    Name = FullCommand.Substring(1, FullCommand.IndexOf(' ') - 1);

                    string remaining = FullCommand.Substring(FullCommand.IndexOf(' ') + 1);

                    while (remaining.Length > 0)
                    {
                        string arg = null;

                        if (remaining[0] == '"')
                        {
                            arg = remaining.Substring(1, remaining.IndexOf('"', 1) - 1);
                            remaining = remaining.Substring(remaining.IndexOf('"', 1) + 1).Trim();
                        }
                        else
                        {
                            if (remaining.Contains(" "))
                            {
                                arg = remaining.Substring(0, remaining.IndexOf(' '));
                                remaining = remaining.Substring(arg.Length).Trim();
                            }
                            else
                            {
                                arg = remaining;
                                remaining = "";
                            }
                        }

                        Args.Add(arg);
                    }
                }
                else
                {
                    Name = FullCommand.Substring(1);
                }
            }
        }
	}
}