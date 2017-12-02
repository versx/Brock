namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Utilities;

    [Command("delete_roles")]
    public class DeleteRolesCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public DeleteRolesCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
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
                Utils.LogError(ex);
                await message.RespondAsync("Failed to delete one or more team roles.");
            }
        }
    }
}