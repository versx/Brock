namespace PokeFilterBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using PokeFilterBot.Utilities;

    public class VersionCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync
            (
                $"{AssemblyUtils.AssemblyName} Version: {AssemblyUtils.AssemblyVersion}\r\n" +
                $"Created by: {AssemblyUtils.CompanyName}\r\n" +
                $"{AssemblyUtils.Copyright}"
            );
        }
    }
}