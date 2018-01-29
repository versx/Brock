namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command(
        Categories.Administrative,
        "Displays how long " + FilterBot.BotName + " has been online for.",
        "\tExample: `.uptime`",
        "uptime"
    )]
    public class UptimeCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

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

            var eb = new DiscordEmbedBuilder();
            eb.AddField("Started:", start.ToString("MM/dd/yyyy hh:mm:ss tt"));
            eb.AddField("Uptime:", Utils.ToReadableString(uptime));
            var embed = eb.Build();

            await message.RespondAsync(string.Empty, false, embed);
        }
    }
}