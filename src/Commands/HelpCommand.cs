namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command("help", "commands", "?")]
    public class HelpCommand : ICustomCommand
    {
        private const string BotName = "Brock";

        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public HelpCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            //TODO: Send basic help command list then allow sub help commands per grouping or by command.
            if (command.HasArgs && command.Args[0].Length == 1)
            {
                //TODO: Parse through provided argument
            }
            else
            {
                //TODO: Send basic help template of other sub help commands.
            }


            await message.RespondAsync
            (
                $"__General Commands__\r\n" +
                "**.team** - Assign yourself to a team role, available teams to join are **Valor**, **Mystic**, and **Instinct**.\r\n" +
                    "\tExample: `.iam Valor` (Joins Valor)\r\n" +
                    "\tExample: `.iam None` (Leave Team)\r\n\r\n" +
                "**.poke** - Simple Pokemon stats lookup.\r\n\r\n" +
                "**.map**/**.maps** - Displays the links to the maps.\r\n\r\n" +
                $"**.setup** - Setup {BotName} (Not Implemented Yet).\r\n\r\n" +
                $"**.demo** - Demos how to use and setup {BotName}.\r\n\r\n" +
                $"**.version** - Display {BotName}'s current version.\r\n\r\n" +
                "**.help** - Shows this help message.\r\n\r\n\r\n" +

                $"__Pokemon Subscriptions__\r\n" +
                "**.add** - Include Pokemon from the specified channels to be notified of.\r\n" +
                    "\tExample: `.add channel1,channel2`\r\n" +
                    "\tExample: `.add channel1`\r\n\r\n" +
                "**.remove** - Removes the selected channels from being notified of Pokemon.\r\n" +
                    "\tExample: `.remove channel1,channel2`\r\n" +
                    "\tExample: `.remove single_channel1`\r\n\r\n" +
                $"**.info** - Shows your current notification subscriptions.\r\n\r\n" +
                "**.sub** - Subscribe to Pokemon notifications via pokedex number.\r\n" +
                    "\tExample: `.sub 147`\r\n" +
                    "\tExample: `.sub 113,242,248`\r\n\r\n" +
                "**.unsub** - Unsubscribe from a one or more or even all subscribed Pokemon notifications.\r\n" +
                    "\tExample: `.unsub 149`\r\n" +
                    "\tExample: `.unsub 3,6,9,147,148,149`\r\n" +
                    "\tExample: `.unsub` (Removes all subscribed Pokemon notifications.)\r\n\r\n" +
                "**.enable** - Activates your Pokemon subscriptions.\r\n\r\n" +
                "**.disable** - Deactivates the Pokemon subscriptions.\r\n\r\n\r\n" +

                $"__Raid Lobby System__\r\n" +
                "**.lobby** - Creates a new raid lobby channel.\r\n" +
                    "\tExample: `.lobby Magikarp_4th 34234234234234`\r\n\r\n" +
                "**.otw** - Notifies users of a specific lobby that you are on the way with x amount of people and ETA.\r\n" +
                    "\tExample: `.otw Magikarp_4th 5mins 3` (Registers that you have 3 people including yourself on the way.)\r\n" +
                    "\tExample: `.otw Magikarp_4th 5mins` (Registers that you are by yourself on the way.)\r\n\r\n" +
                "**.here** - Checks you into the specified raid lobby informing you are now at the raid.\r\n" +
                    "\tExample: `.here Magikarp_4th`\r\n\r\n" +
                "**.cancel** - Cancel your .otw/.here command.\r\n"
                //$"__Owner Commands__\r\n" +
                //$"**.setup** - \r\n" +
                //"**.create_roles** - Creates the required team roles to be assigned when users type the `.team <team>` commmand.\r\n" +
                //$"**.delete_roles** - Deletes all team roles that the {BotName} created.\r\n"
                //$"**.restart** - Restarts {BotName}.\r\n" +
                //$"**.shutdown** - Shuts down {BotName}\r\n.
            );
        }
    }
}
/*
Thunderbot - bang lure pls

Alias:
  alias      Manage per-server aliases for commands
CustomCommands:
  customcom  Custom commands management
General:
  8          Ask 8 ball a question
  choose     Chooses between multiple choices.
  flip       Flips a coin... or a user.
  lmgtfy     Creates a lmgtfy link
  poll       Starts/stops a poll
  roll       Rolls random number (between 1 and user choice)
  rps        Play rock paper scissors
  serverinfo Shows server's informations
  stopwatch  Starts/stops stopwatch
  urban      Urban Dictionary search
  userinfo   Shows users's informations
Owner:
  contact    Sends a message to the owner
  info       Shows info about Red
  set        Changes Red's core settings
  uptime     Shows Red's uptime
  version    Shows Red's current version
Trivia:
  trivia     Start a trivia session with the specified list
blame:
  blame      Blame people!
checkproxy:
  checkproxy Checks the provided proxy.
sprite:
  cries      
  sprite     
​No Category:
  help       Shows this message.

Type !help command for more info on a command.
You can also type !help category for more info on a category.
 */
