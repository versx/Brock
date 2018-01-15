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
        "\tExample: `.restart`",    
        "restart"
    )]
    public class RestartCommand : ICustomCommand
    {
        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public RestartCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
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