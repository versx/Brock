namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.General,
        "Displays how long " + Strings.BotName + " has been online for.",
        "\tExample: `.uptime`",
        "uptime"
    )]
    public class UptimeCommand : ICustomCommand
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public UptimeCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return;

            var start = Process.GetCurrentProcess().StartTime;
            var now = DateTime.Now;
            var uptime = now.Subtract(start);

            var eb = new DiscordEmbedBuilder { Color = DiscordColor.Green, Title = $"{Strings.BotName} Uptime" };
            eb.AddField("Started:", start.ToString("MM/dd/yyyy hh:mm:ss tt"), true);
            eb.AddField("Uptime:", uptime.ToReadableString(),  true);

            var embed = eb.Build();
            if (embed == null) return;

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}