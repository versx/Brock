namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command("uptime")]
    public class UptimeCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public UptimeCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return; //REVIEW: Should command be processed even with unnecessary parameters.

            var start = Process.GetCurrentProcess().StartTime;
            var now = DateTime.Now;
            var uptime = now.Subtract(start);
            //var uptimeMessage = string.Empty;

            //if (uptime.Days > 0)
            //{
            //    uptimeMessage += $"{uptime.Days} days, ";
            //}
            //if (uptime.Hours > 0)
            //{
            //    uptimeMessage += $"{uptime.Hours} hours, ";
            //}
            //if (uptime.Minutes > 0)
            //{
            //    uptimeMessage += $"{uptime.Minutes} minutes, ";
            //}
            //if (uptime.Seconds > 0)
            //{
            //    uptimeMessage += $"{uptime.Seconds} seconds";
            //}

            await message.RespondAsync(Utils.ToReadableString(uptime), false, CreateEmbed("Test Embed"));
        }

        public DiscordEmbed CreateEmbed(string title)
        {
            var eb = new DiscordEmbedBuilder
            {
                //Author = new DiscordEmbedBuilder.EmbedAuthor()
                //{
                //    Name = "versx#8151"
                //},
                Title = title
            };

            var about = eb.AddField
            (
                "About Brock Bot", 
                "Brock Bot is a simple Discord bot that allows you to assign yourself to your Pokemon team, create Raid Lobbies, filter sponsor raids, Pokemon spawn notifier and more. ",
                true
            );

            var dev = eb.AddField
            (
                "Developer",
                "versx#8151"
            );

            var git = eb.AddField
            (
                "GitHub Repository",
                "https://github.com/versx/Brock\n\nTo make a suggestion or report a bug regarding Brock, " +
                "go to this repository and use the issue tab to create an issue or mesage me on discord @ versx#8151."
            );

            var footer = eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = "Version " + AssemblyUtils.AssemblyVersion
            };

            return eb.Build();
        }
    }
}