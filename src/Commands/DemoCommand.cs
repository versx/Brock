namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    using PokeFilterBot.Utilities;

    public class DemoCommand : ICustomCommand
    {
        public bool AdminCommand => false;

        public async Task Execute(DiscordMessage message, Command command)
        {
            await message.RespondAsync($"Below is a demo of how to operate {AssemblyUtils.AssemblyName}:");
            await message.RespondAsync(".setup upland_rares,upland_ultra,ontario_ultra");
            await message.RespondAsync(".sub 1,147,148,149,246,247,248");
            await message.RespondAsync(".unsub 1");
            await message.RespondAsync(".remove upland_rares");
            await message.RespondAsync(".enable");
            await message.RespondAsync(".info");
        }
    }
}