namespace BrockBot.Commands
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using BrockBot.Utilities;

    public class RestartCommand : ICustomCommand
    {
        public bool AdminCommand => true;

        public async Task Execute(DiscordMessage message, Command command)
        {
            //Application.Restart();

            Process.Start(AssemblyUtils.AssemblyPath);
            Environment.Exit(0);
            
            await Task.CompletedTask;
        }
    }
}