namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.Administrative,
        "Setup " + FilterBot.BotName + " (Not Implemented Yet)",
        null,
        "setup"
    )]
    public class SetupCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public SetupCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            switch (command.Args.Count)
            {
                case 1:

                    break;
                case 2:
                    
                    break;
                default:
                    await message.RespondAsync("Invalid configuration command, here is a list of available options:\r\n.setup teams");
                    break;
            }

            await Task.CompletedTask;
        }
    }
}