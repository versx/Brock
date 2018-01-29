namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Administrative,
        "Sets configuration setting values directly.",
        "\tExample: .set prefix !\r\n" +
        "\tExample: .set team_assignment true",
        "set"
    )]
    public sealed class SetCommand : ICustomCommand
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly Config _config;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public SetCommand(DiscordClient client, IDatabase db, Config config)
        {
            _client = client;
            _db = db;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            var setting = command.Args[0];
            var value = command.Args[1];

            switch (setting)
            {
                case "owner":
                    #region
                    if (!ulong.TryParse(value, out ulong newOwnerId))
                    {
                        await message.RespondAsync($"{value} is not a valid user id.");
                        return;
                    }

                    var newOwner = await _client.GetUser(newOwnerId);
                    if (newOwner == null)
                    {
                        await message.RespondAsync($"{value} is not a valid user id.");
                        return;
                    }

                    _config.OwnerId = newOwnerId;
                    break;
                    #endregion
                case "command-channel":
                    #region
                    if (!ulong.TryParse(value, out ulong newCommandChannelId))
                    {
                        await message.RespondAsync($"{value} is not a valid channel id.");
                        return;
                    }

                    var newChannel = await _client.GetChannelAsync(newCommandChannelId);
                    if (newChannel == null)
                    {
                        await message.RespondAsync($"{value} is not a valid channel id.");
                        return;
                    }

                    _config.CommandsChannelId = Convert.ToUInt64(value);
                    break;
                    #endregion
                case "prefix":
                    #region
                    _config.CommandsPrefix = value[0];
                    break;
                    #endregion
                case "notify-joined":
                    #region
                    if (!bool.TryParse(value, out bool notifyJoined))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.NotifyNewMemberJoined = notifyJoined;
                    break;
                #endregion
                case "notify-left":
                    #region
                    if (!bool.TryParse(value, out bool notifyLeft))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.NotifyMemberLeft = Convert.ToBoolean(value);
                    break;
                #endregion
                case "notify-banned":
                    #region
                    if (!bool.TryParse(value, out bool notifyBanned))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.NotifyMemberBanned = notifyBanned;
                    break;
                #endregion
                case "notify-unbanned":
                    #region
                    if (!bool.TryParse(value, out bool notifyUnbanned))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.NotifyMemberUnbanned = notifyUnbanned;
                    break;
                #endregion
                case "enable-team-assignment":
                    #region
                    if (!bool.TryParse(value, out bool teamAssignment))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.AllowTeamAssignment = teamAssignment;
                    break;
                    #endregion
                case "enable-welcome":
                    #region
                    if (!bool.TryParse(value, out bool sendWelcome))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.SendWelcomeMessage = sendWelcome;
                    break;
                    #endregion
                case "welcome-msg":
                    #region
                    _config.WelcomeMessage = value;
                    break;
                    #endregion
                case "enable-twitter":
                    #region
                    if (!bool.TryParse(value, out bool sendTwitter))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.TwitterUpdates.PostTwitterUpdates = sendTwitter;
                    break;
                    #endregion
                case "enable-advertisement":
                    #region
                    if (!bool.TryParse(value, out bool sendAdvertisement))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.Advertisement.Enabled = sendAdvertisement;
                    break;
                #endregion
                case "assign-new-members-city-roles":
                    #region
                    if (!bool.TryParse(value, out bool assignNewMembersCityRoles))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.AssignNewMembersCityRoles = assignNewMembersCityRoles;
                    break;
                    #endregion
                case "enable-raid-lobbies":
                    #region
                    if (!bool.TryParse(value, out bool raidLobbies))
                    {
                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
                        return;
                    }

                    _config.RaidLobbies.Enabled = raidLobbies;
                    break;
                    #endregion
            }

            _config.Save();

            await message.RespondAsync($"{message.Author.Mention}, Setting {setting} was set to {value}.");
        }
    }
}