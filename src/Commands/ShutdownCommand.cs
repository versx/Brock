namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public class ShutdownCommand : ICustomCommand
    {
        public bool AdminCommand => true;

        public async Task Execute(DiscordMessage message, Command command)
        {
            Environment.Exit(0);
            await Task.CompletedTask;
        }
    }
}