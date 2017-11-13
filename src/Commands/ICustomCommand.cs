namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public interface ICustomCommand
    {
        bool AdminCommand { get; }

        Task Execute(DiscordMessage message, Command command);
    }
}