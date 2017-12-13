namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(Categories.General,
        "Displays various information about the current guild.",
        "",
        //CommandPermissionLevel.User,
        "serverinfo"
    )]
    public class ServerInfoCommand : ICustomCommand
    {
        private readonly Config _config;

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public ServerInfoCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.IsDirectMessageSupported();

            try
            {
                var guild = message.Channel.Guild;
                if (guild == null)
                {

                }

                var avatarURL = guild.IconUrl ?? "http://ravegames.net/ow_userfiles/themes/theme_image_22.jpg";
                var created = guild.CreationTimestamp;
                var eb = new DiscordEmbedBuilder()
                {
                    Color = new DiscordColor(4, 97, 247),
                    ThumbnailUrl = (avatarURL),
                    Title = $"{guild.Name} Guild Information",
                    Description = $"Created {(int)(DateTime.Now.Subtract(created.DateTime).TotalDays)} days ago on {created.ToString().Remove(created.ToString().Length - 6)}.",
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text = $"Requested by {message.Author.Username}#{message.Author.Discriminator} | Guild ID: {guild.Id}",
                        IconUrl = message.Author.GetAvatarUrl(ImageFormat.Png)
                    }
                };
                //await guild.DownloadUsersAsync();
                //var onlineCount = users.Count(u => u.Status != UserStatus.Unknown && u.Status != UserStatus.Invisible && u.Status != UserStatus.Offline);

                var guildOwner = await guild.GetMemberAsync(_config.OwnerId);
                int online = 0;
                foreach (var member in guild.Members)
                {
                    if (member.Presence.Status != UserStatus.Invisible && member.Presence.Status != UserStatus.Offline)
                    {
                        online++;
                    }
                }

                eb.AddField("Owner", guildOwner.Username, true);
                eb.AddField("Members", $"{online} / {guild.MemberCount}", true);
                eb.AddField("Region", guild.RegionId.ToUpper(), true);
                eb.AddField("Roles", guild.Roles.Count.ToString("N0"), true);

                int voice = 0;
                int text = 0;
                foreach (var channel in guild.Channels)
                {
                    switch (channel.Type)
                    {
                        case ChannelType.Text:
                            text++;
                            break;
                        case ChannelType.Voice:
                            voice++;
                            break;
                    }
                }
                eb.AddField("Channels", $"{text} text, {voice} voice", true);
                eb.AddField("AFK Channel", $"{(guild.AfkChannel == null ? $"No AFK Channel" : $"{guild.AfkChannel.Name}\n*in {(int)(guild.AfkTimeout / 60)} Min*")}", true);
                eb.AddField("Total Emojis", $"{guild.Emojis.Count}", true);
                eb.AddField("Avatar Url", $"[Click to view]({avatarURL})", true);

                string emojis = "";
                foreach (var emoji in guild.Emojis)
                {
                    if (emojis.Length < 950)
                        emojis += $"<:{emoji.Name}:{emoji.Id}> ";
                }
                //x.Value = $"{String.Format(":{0}:",String.Join(": , :", Context.Guild.Emojis))}";//await Context.Channel.SendMessageAsync(String.Format("Videos: \n{0}\n", String.Join("\n", videos)));
                if (string.IsNullOrEmpty(emojis)) emojis = "None";
                eb.AddField("Emojis", emojis, false);

                var embed = eb.Build();

                await message.RespondAsync(string.Empty, false, embed);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
            }
        }
    }
}