namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    [Command(
        Categories.General,
        "Provides a link to a search query via Let Me Google That For You (lmgtfy).",
        "\tExample: `.google \"let me google that for you\"",
        "google"
    )]
    public class GoogleCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var cmd = command.Args[0];
            cmd = cmd.Replace(" ", "%20");

            await message.RespondAsync($"<https://lmgtfy.com/?q={cmd}>");
        }
    }
}