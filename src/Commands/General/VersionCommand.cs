namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.General,
        "Display " + FilterBot.BotName + "'s current version.",
        null,
        "v", "ver", "version", "about"
    )]
    public class VersionCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public VersionCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync(string.Empty, false, CreateEmbed());
        }

        public DiscordEmbed CreateEmbed()
        {
            var eb = new DiscordEmbedBuilder();
            eb.AddField
            (
                $"About {FilterBot.BotName} Bot",
                $"{FilterBot.BotName} Bot is a simple Discord bot that allows you to assign yourself to your Pokemon team, create Raid Lobbies, filter sponsored raids, Pokemon spawn notifier and more."
            );
            eb.AddField("Developer", "versx#8151");
            eb.AddField("Debugger & Reviewer", "Boracyk#7608");
            eb.AddField("Company", AssemblyUtils.CompanyName);
            eb.AddField("Discord", "https://discord.me/versx");
            eb.AddField
            (
                "GitHub Repository",
                "https://github.com/versx/Brock\n\nTo make a suggestion or report a bug regarding Brock, " +
                "go to the GitHub repository and use the issue tab to create an issue or mesage me on Discord @ versx#8151."
            );
            eb.WithFooter($"{AssemblyUtils.Copyright}, Version {AssemblyUtils.AssemblyVersion}");

            var embed = eb.Build();
            return embed;
        }
    }
}