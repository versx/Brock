namespace BrockBot.Commands
{
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    [Command(
        Categories.Voting,
        "",
        "\tExample: `.poll \"This is a voting poll question?\" Answer1 Answer2 Answer3`",
        "poll"
    )]
    public class CreateVoteCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public async Task Execute(DiscordMessage message, Command command)
        {
            await Task.CompletedTask;
        }
    }
}