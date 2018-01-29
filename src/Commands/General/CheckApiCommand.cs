namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.General,
        "Checks what the current map scanner version is against the latest Pokemon Go API version.",
        "\tExample: `.checkapi`",
        "checkapi"
    )]
    public class CheckApiCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public CheckApiCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var eb = new DiscordEmbedBuilder { Title = "Pokemon Go API Version Check:" };
            var current = _config.ScannerApiVersion;
            var latest = Utils.GetPoGoApiVersion();
            var isLatest = IsVersionMatch(current, latest);
            eb.AddField("Current:", current.ToString(), true);
            eb.AddField("Latest:", latest.ToString(), true);
            eb.WithFooter(current == latest ? "LATEST API VERSION" : "NEW POGO API RELEASED");
            eb.Color = isLatest ? DiscordColor.Green : DiscordColor.Red;

            var embed = eb.Build();
            await message.RespondAsync(string.Empty, false, embed);
        }

        private bool IsVersionMatch(Version current, Version latest)
        {
            return
                current.Major == latest.Major &&
                current.Minor == latest.Minor &&
                current.Build == latest.Build;
        }
    }
}