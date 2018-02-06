namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus.Entities;

    [Command(
        Categories.General,
        "Shows the specified user the door (Jokingly).",
        "\tExample: `.door`\r\n" +
        "\tExample: `.door @mention`",
        "door", "out"
    )]
    public class DoorCommand : ICustomCommand
    {
        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs)
            {
                if (command.Args.Count != 1) return;

                var mention = command.Args[0];
                await message.RespondAsync($"{mention} :point_right: :door:");
                return;
            }

            await message.RespondAsync($":point_right: :door:");
        }
    }
}