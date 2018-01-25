namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;

    [Command(
        Categories.Administrative,
        "Deletes all team roles that " + FilterBot.BotName + " has created.",
        "\tExample: `.delete-roles`",
        "delete-roles"
    )]
    public class DeleteRolesCommand : ICustomCommand
    {
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public DeleteRolesCommand(DiscordClient client, IDatabase db, IEventLogger logger)
        {
            Client = client;
            Db = db;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            try
            {
                foreach (var role in message.Channel.Guild.Roles)
                {
                    if (Roles.Teams.ContainsKey(role.Name))
                    {
                        await message.Channel.Guild.DeleteRoleAsync(role);
                    }
                }

                await message.RespondAsync("All team roles have been deleted.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                await message.RespondAsync("Failed to delete one or more team roles.");
            }
        }
    }
}