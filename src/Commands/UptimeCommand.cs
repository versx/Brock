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

            await message.RespondAsync(uptimeMessage, false, CreateEmbed("Test Embed"));
        }

        public DiscordEmbed CreateEmbed(string title)
        {
            var eb = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    Name = "versx#8151"
                },
                Title = title
            };

            var about = eb.AddField
            (
                "About PoGo Bot", "PoGo Bot is a simple Discord bot that allows you to automatically set your team by using Discord roles. " +
                "It has potential to have more functionality to come.",
                true
            );

            var dev = eb.AddField
            (
                "Developer",
                "versx#8151"
            );

            var git = eb.AddField
            (
                "Github Repositoy",
                "https://github.com/versx/PokeFilterBot" + "\n\nTo make a suggestion or report a bug, " +
                "go to this repository and use the issue tab to create an issue. Or you can mesage me on discord."
            );

            var footer = eb.Footer = new DiscordEmbedBuilder.EmbedFooter
            {
                Text = "Version " + AssemblyUtils.AssemblyVersion
            };
            var embed = eb.Build();
            return embed;
        }
    }
}