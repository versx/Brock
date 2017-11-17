namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public class UptimeCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return; //REVIEW: Should command be processed even with unnecessary parameters.

            var start = Process.GetCurrentProcess().StartTime;
            var now = DateTime.Now;
            var uptime = now.Subtract(start);
            var uptimeMessage = string.Empty;

            if (uptime.Days > 0)
            {
                uptimeMessage += $"{uptime.Days} days";
            }
            if (uptime.Hours > 0)
            {
                uptimeMessage += $"{uptime.Hours} hours";
            }
            if (uptime.Minutes > 0)
            {
                uptimeMessage += $"{uptime.Minutes} minutes";
            }
            if (uptime.Seconds > 0)
            {
                uptimeMessage += $"{uptime.Seconds} seconds";
            }

            await message.RespondAsync(uptimeMessage);
        }
    }
}