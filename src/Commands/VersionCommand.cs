namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.Info,
        "Display " + FilterBot.BotName + "'s current version.",
        null,
        "v", "ver", "version", "about"
    )]
    public class VersionCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public VersionCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync(string.Empty, false, CreateEmbed());
        }

        public DiscordEmbed CreateEmbed()
        {
            var eb = new DiscordEmbedBuilder
            {
                //Title = AssemblyUtils.AssemblyName
            };

            eb.AddField
            (
                $"About {FilterBot.BotName} Bot",
                $"{FilterBot.BotName} Bot is a simple Discord bot that allows you to assign yourself to your Pokemon team, create Raid Lobbies, filter sponsor raids, Pokemon spawn notifier and more."
            );

            eb.AddField
            (
                "Developer",
                "versx#8151"
            );

            eb.AddField
            (
                "Company",
                AssemblyUtils.CompanyName
            );

            eb.AddField
            (
                "GitHub Repository",
                "https://github.com/versx/Brock\n\nTo make a suggestion or report a bug regarding Brock, " +
                "go to the GitHub repository and use the issue tab to create an issue or mesage me on Discord @ versx#8151."
            );

            eb.WithFooter(AssemblyUtils.Copyright + ", Version " + AssemblyUtils.AssemblyVersion);

            return eb.Build();
        }
    }
}