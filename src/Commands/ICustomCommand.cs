namespace PokeFilterBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public interface ICustomCommand
    {
        Task Execute(DiscordMessage message, Command command);
    }
}