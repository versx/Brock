namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command("set")]
    public sealed class SetCommand : ICustomCommand
    {
        private readonly Config _config;

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public SetCommand(Config config)
        {
            _config = config;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            var setting = command.Args[0];
            var value = command.Args[1];

            switch (setting)
            {
                case "owner":
                    //TODO: Check if provided user exists.
                    _config.OwnerId = Convert.ToUInt64(value);
                    break;
                case "command_channel":
                    //TODO: Check if provided commands channel exists.
                    _config.CommandsChannel = value;
                    break;
                case "prefix":
                    _config.CommandsPrefix = value[0];
                    break;
                case "notify_joined":
                    _config.NotifyNewMemberJoined = Convert.ToBoolean(value);
                    break;
                case "notify_left":
                    _config.NotifyMemberLeft = Convert.ToBoolean(value);
                    break;
                case "notify_banned":
                    _config.NotifyMemberBanned = Convert.ToBoolean(value);
                    break;
                case "notify_unbanned":
                    _config.NotifyMemberUnbanned = Convert.ToBoolean(value);
                    break;
                case "team_assignment":
                    _config.AllowTeamAssignment = Convert.ToBoolean(value);
                    break;
                case "welcome":
                    _config.SendWelcomeMessage = Convert.ToBoolean(value);
                    break;
                case "welcome_msg":
                    _config.WelcomeMessage = value;
                    break;
            }

            _config.Save();

            //await message.RespondAsync("");
            await Task.CompletedTask;
        }
    }
}