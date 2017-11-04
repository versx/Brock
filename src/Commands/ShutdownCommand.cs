namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public class ShutdownCommand : ICustomCommand
    {
        public async Task Execute(DiscordMessage message, Command command)
        {
            Environment.Exit(0);
            await Task.CompletedTask;
        }
    }
}