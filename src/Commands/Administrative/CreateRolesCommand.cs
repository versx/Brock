namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    [Command(
        Categories.Administrative,
        "Creates the required team roles to be assigned when users type the team assignment commmand.",
        null,
        "create_roles"
    )]
    public class CreateRolesCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

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
                            var newRole = await message.Channel.Guild.CreateRoleAsync(team.Key, message.Channel.Guild.EveryoneRole.Permissions, team.Value, null, true);
                            if (newRole == null)
                            {
                                Utils.LogError(new Exception($"Failed to create team role {team.Key}"));
                                return;
                            }

                            var parentChannel = Client.GetChannelByName("DISCUSSIONS");
                            if (parentChannel == null)
                            {
                                Utils.LogError(new Exception($"Failed to find parent channel 'DISCUSSIONS'."));
                                return;
                            }

                            var newChannel = await message.Channel.Guild.CreateChannelAsync($"team_{team.Key.ToLower()}", ChannelType.Text, parentChannel);
                            if (newChannel == null)
                            {
                                Utils.LogError(new Exception($"Failed to create team channel team_{team.Key.ToLower()}"));
                                return;
                            }

                            await newChannel.GrantPermissions(message.Channel.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages | Permissions.ReadMessageHistory);
                            await newChannel.GrantPermissions(newRole, Permissions.SendMessages | Permissions.ReadMessageHistory, Permissions.None);
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