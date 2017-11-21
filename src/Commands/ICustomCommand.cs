namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    public interface ICustomCommand
    {
        bool AdminCommand { get; }

        DiscordClient Client { get; }

        IDatabase Db { get; }

        Task Execute(DiscordMessage message, Command command);
    }
}