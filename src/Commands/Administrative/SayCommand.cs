namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    [Command(
        Categories.Administrative,
        "Says something to the specified channel.",
        "\tExample: .say general \"Hey how's it going everyone?\"\r\n" +
        "\tExample: .say announcements \"Today will be a sunny day!\"",
        "say"
    )]
    public class SayCommand : ICustomCommand
    {
        private readonly IEventLogger _logger;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        #endregion

        #region Constructor

        public SayCommand(DiscordClient client, IDatabase db, Config config, IEventLogger logger)
        {
            Client = client;
            Db = db;
            Config = config;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            await message.IsDirectMessageSupported();

            var channelName = command.Args[0];
            var channel = Client.GetChannelByName(channelName);
            if (channel == null)
            {
                await message.RespondAsync($"Failed to lookup channel {channelName}.");
                return;
            }
            
            try
            {
                var msg = command.Args[1];
                await channel.SendMessageAsync(msg);
                //await Client.SendMessageAsync(channel, msg);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}