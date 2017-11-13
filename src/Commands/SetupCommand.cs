namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public class SetupCommand : ICustomCommand
    {
        public bool AdminCommand => true;

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