namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public class MapCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs) return; //REVIEW: Should the command proceed even with unnecessary parameters?

            await message.RespondAsync($"Pokemon map: https://pokemap.ver.sx\r\nGyms & Raids map: https://gymmap.ver.sx");
        }
    }
}