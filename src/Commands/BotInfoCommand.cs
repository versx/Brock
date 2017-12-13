namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(Categories.General,
        "Displays various information about Brock.",
        "",
        "botinfo"
    )]
    public class BotInfoCommand : ICustomCommand
    {
        private const string GitHubLink = "https://GitHub.com/versx/Brock";
        private const string DiscordLink = "https://discord.gg/6RNRtyE";

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public BotInfoCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            var proc = Process.GetCurrentProcess();
            var eb = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor(4, 97, 247),
                ThumbnailUrl = Client.CurrentUser.GetAvatarUrl(ImageFormat.Png),
                Footer = new DiscordEmbedBuilder.EmbedFooter()
                {
                    Text = $"Requested by {message.Author.Username}#{message.Author.Discriminator}",
                    IconUrl = message.Author.GetAvatarUrl(ImageFormat.Png)
                },
                Title = $"**{FilterBot.BotName} Info**",
                Url = GitHubLink
            };
            eb.AddField("Uptime", (DateTime.Now - proc.StartTime).ToString(@"d'd 'hh\:mm\:ss"), true);
            eb.AddField(".NET Framework", RuntimeInformation.FrameworkDescription, true);
            //eb.AddField("Used RAM", $"{(proc.PagedMemorySize64 == 0 ? $"{RSS.ToString("f1")} mB / {VSZ.ToString("f1")} mB" : $"{formatRamValue(proc.PagedMemorySize64).ToString("f2")} {formatRamUnit(proc.PagedMemorySize64)} / {formatRamValue(proc.VirtualMemorySize64).ToString("f2")} {formatRamUnit(proc.VirtualMemorySize64)}")}", true);
            //eb.AddField("Commands Executed", $"{_commandHandler.CommandsRunSinceRestart()} since restart", true);
            //eb.AddField("Threads running", $"{((IEnumerable)proc.Threads).OfType<ProcessThread>().Where(t => t.ThreadState == ThreadState.Running).Count()} / {proc.Threads.Count}", true);
            eb.AddField("Connected Guilds", $"{Client.Guilds.Count}", true);
            var channelCount = 0;
            var userCount = 0;
            foreach (var g in Client.Guilds)
            {
                channelCount += g.Value.Channels.Count;
                userCount += g.Value.Members.Count;
            }
            eb.AddField("Watching Channels", $"{channelCount}", true);
            eb.AddField("Users with access", $"{userCount}", true);
            //eb.AddField("Ping", $"{Client.Latency} ms", true);
            eb.AddField($"Brock's Official Guild", $"[Feedback & Suggestions]({DiscordLink})", true);

            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}