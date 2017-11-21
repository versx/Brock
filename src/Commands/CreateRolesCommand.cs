namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command("create_roles")]
    public class CreateRolesCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public CreateRolesCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            foreach (var team in Roles.Teams)
            {
                try
                {
                    if (Client.GetRoleFromName(team.Key) == null)
                    {
                        if (message.Channel.Guild != null)
                        {
                            await message.Channel.Guild.CreateRoleAsync(team.Key, message.Channel.Guild.EveryoneRole.Permissions, team.Value, null, true, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError(ex);
                    await message.RespondAsync($"Failed to create team role {team.Key}, it might already exist or I do not have the correct permissions to manage roles.");
                }
            }

            await message.RespondAsync("Valor, Mystic, and Instinct team roles were successfully created.");
        }
    }
}