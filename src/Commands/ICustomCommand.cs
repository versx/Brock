namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    public interface ICustomCommand
    {
        CommandPermissionLevel PermissionLevel { get; }

        Task Execute(DiscordMessage message, Command command);
    }
}