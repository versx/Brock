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
        "Checks what the current map scanner version is against the latest Pokemon Go api version.",
        "\tExample: `.checkapi`",
        "checkapi"
    )]
    public class CheckApiCommand : ICustomCommand
    {
        private readonly Config _config;

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public CheckApiCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var eb = new DiscordEmbedBuilder { Title = "Pokemon Go Api Version Check:" };
            var current = _config.ScannerApiVersion;
            var latest = Utils.GetPoGoApiVersion();
            eb.AddField("Current:", current.ToString(), true);
            eb.AddField("Latest:", latest.ToString(), true);
            eb.WithFooter(current == latest ? "LATEST API VERSION" : "NEW POGO API RELEASED");
            eb.Color = current == latest ? DiscordColor.Green : DiscordColor.Red;

            var embed = eb.Build();
            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}