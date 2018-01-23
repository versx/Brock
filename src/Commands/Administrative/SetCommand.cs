//namespace BrockBot.Commands
//{
//    using System;
//    using System.Threading.Tasks;

//    using DSharpPlus;
//    using DSharpPlus.Entities;

//    using BrockBot.Configuration;
//    using BrockBot.Data;

//    [Command(
//        Categories.Administrative,
//        "Sets configuration setting values directly.",
//        "\tExample: .set prefix !\r\n" +
//        "\tExample: .set team_assignment true",
//        "set"
//    )]
//    public sealed class SetCommand : ICustomCommand
//    {
//        #region Variables

//        private readonly Config _config;

//        #endregion

//        #region Properties

//        public bool AdminCommand => true;

//        public DiscordClient Client { get; }

//        public IDatabase Db { get; }

//        #endregion

//        #region Constructor

//        public SetCommand(Config config)
//        {
//            _config = config;
//        }

//        #endregion

//        public async Task Execute(DiscordMessage message, Command command)
//        {
//            if (!command.HasArgs) return;
//            if (command.Args.Count != 2) return;

//            var setting = command.Args[0];
//            var value = command.Args[1];

//            switch (setting)
//            {
//                case "owner":
//                    #region
//                    if (!ulong.TryParse(value, out ulong newOwnerId))
//                    {
//                        await message.RespondAsync($"{value} is not a valid user id.");
//                        return;
//                    }

//                    var newOwner = await Client.GetUser(newOwnerId);
//                    if (newOwner == null)
//                    {
//                        await message.RespondAsync($"{value} is not a valid user id.");
//                        return;
//                    }

//                    _config.OwnerId = newOwnerId;
//                    break;
//                    #endregion
//                case "command_channel":
//                    #region
//                    if (!ulong.TryParse(value, out ulong newCommandChannelId))
//                    {
//                        await message.RespondAsync($"{value} is not a valid channel id.");
//                        return;
//                    }

//                    var newChannel = await Client.GetChannelAsync(newCommandChannelId);
//                    if (newChannel == null)
//                    {
//                        await message.RespondAsync($"{value} is not a valid channel id.");
//                        return;
//                    }

//                    _config.CommandsChannelId = Convert.ToUInt64(value);
//                    break;
//                    #endregion
//                case "prefix":
//                    _config.CommandsPrefix = value[0];
//                    break;
//                case "notify_joined":
//                    #region
//                    if (!bool.TryParse(value, out bool notifyJoined))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.NotifyNewMemberJoined = notifyJoined;
//                    break;
//                    #endregion
//                case "notify_left":
//                    #region
//                    if (!bool.TryParse(value, out bool notifyLeft))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.NotifyMemberLeft = Convert.ToBoolean(value);
//                    break;
//                    #endregion
//                case "notify_banned":
//                    #region
//                    if (!bool.TryParse(value, out bool notifyBanned))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.NotifyMemberBanned = notifyBanned;
//                    break;
//                    #endregion
//                case "notify_unbanned":
//                    #region
//                    if (!bool.TryParse(value, out bool notifyUnbanned))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.NotifyMemberUnbanned = notifyUnbanned;
//                    break;
//                    #endregion
//                case "team_assignment":
//                    #region
//                    if (!bool.TryParse(value, out bool teamAssignment))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.AllowTeamAssignment = teamAssignment;
//                    break;
//                    #endregion
//                case "welcome":
//                    #region
//                    if (!bool.TryParse(value, out bool sendWelcome))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.SendWelcomeMessage = sendWelcome;
//                    break;
//                    #endregion
//                case "welcome_msg":
//                    _config.WelcomeMessage = value;
//                    break;
//                case "twitter":
//                    #region
//                    if (!bool.TryParse(value, out bool sendTwitter))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.TwitterUpdates.PostTwitterUpdates = sendTwitter;
//                    break;
//                    #endregion
//                case "advertisement":
//                    #region
//                    if (!bool.TryParse(value, out bool sendAdvertisement))
//                    {
//                        await message.RespondAsync($"{value} is not a valid value, please use true or false.");
//                        return;
//                    }

//                    _config.Advertisement.Enabled = sendAdvertisement;
//                    break;
//                    #endregion
//            }

//            _config.Save();

//            //await message.RespondAsync("");
//            await Task.CompletedTask;
//        }
//    }
//}