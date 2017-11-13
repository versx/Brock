namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public class HelpCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                "**.team** - Assign yourself to a team role, available teams to join are the following: Valor, Mystic, or Instinct. You can only join one team at a time, type `.team None` to leave a team.\r\n" +
                    "\tExample: `.iam Valor`\r\n" +
                    "\tExample: `.iam None`\r\n\r\n" +
                $"**.info** - Shows the your current Pokemon subscriptions and which channels to listen to.\r\n\r\n" +
                "**.setup** - Include Pokemon from the specified channels to be notified of.\r\n" +
                    "\tExample: `.setup channel1,channel2`\r\n" +//34293948729384,3984279823498\r\n" + 
                    "\tExample: `.setup channel1`\r\n\r\n" +//34982374982734\r\n" +
                "**.remove** - Removes the selected channels from being notified of Pokemon.\r\n" +
                    "\tExample: `.remove channel1,channel2`\r\n" +
                    "\tExample: `.remove single_channel1`\r\n\r\n" +
                //".subs - Lists all Pokemon subscriptions.\r\n" +
                "**.sub** - Subscribe to Pokemon notifications via pokedex number.\r\n" +
                    "\tExample: `.sub 147`\r\n" +
                    "\tExample: `.sub 113,242,248`\r\n\r\n" +
                "**.unsub** - Unsubscribe from a single or multiple Pokemon notification or even all subscribed Pokemon notifications.\r\n" +
                    "\tExample: `.unsub 149`\r\n" +
                    "\tExample: `.unsub 3,6,9,147,148,149`\r\n" +
                    "\tExample: `.unsub` (Removes all subscribed Pokemon notifications.)\r\n\r\n" +
                "**.enable** - Activates the Pokemon notification subscriptions.\r\n\r\n" +
                "**.disable** - Deactivates the Pokemon notification subscriptions.\r\n\r\n" +
                $"**.demo** - Display a demo of the {AssemblyUtils.AssemblyName}.\r\n\r\n" +
                $"**.version** - Display the current {AssemblyUtils.AssemblyName} version.\r\n\r\n" +
                "**.help** - Shows this help message.\r\n\r\n" +
                $"Raid Lobby System:\r\n" +
                "**.lobby** - Creates a new raid lobby channel.\r\n" +
                    "\tExample: `.lobby Magikarp_4th 34234234234234`\r\n" +
                "**.ontheway** - Notifies people in the specified lobby that you are on the way with x amount of people and ETA.\r\n" +
                    "\tExample: `.ontheway Magikarp_4th 5mins 3` (Registers that you have 3 people including yourself on the way.\r\n" +
                    "\tExample: `.ontheway Magikarp_4th 5mins` (Registers that you are by yourself on the way.\r\n" +
                "**.checkin** - Checks you into the specified raid lobby notifying everyone that you have arrived to the raid location.\r\n" +
                    "\tExample: `.checkin Magikarp_4th`"//\r\n\r\n" +
                //$"Admin Only Commands:\r\n" +
                //"**.create_roles** - Creates the required team roles to be assigned when users type the `.iam <team>` commmand.\r\n" +
                //$"**.delete_roles** - Deletes all team roles that the {AssemblyUtils.AssemblyName} created.\r\n"
            );
        }
    }
}