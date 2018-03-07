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
        "Restarts " + FilterBot.BotName + ".",
        "\tExample: `.reboot`",
        "reboot"
    )]
    public class RebootCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public RebootCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            //Application.Restart();

            Process.Start(AssemblyUtils.AssemblyPath);
            Environment.Exit(0);
            
            await Task.CompletedTask;
        }
    }
}