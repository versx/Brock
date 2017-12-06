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
        "Says something to the specified channel.",
        "\tExample: .say general \"Hey how's it going everyone?\"\r\n" +
        "\tExample: .say announcements \"Today will be a sunny day!\"",
        "say"
    )]
    public class SayCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        public Config Config { get; }

        #endregion

        #region Constructor

        public SayCommand(DiscordClient client, IDatabase db, Config config)
        {
            Client = client;
            Db = db;
            Config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 2) return;

            if (message.Channel == null)
            {
                await message.RespondAsync("DM is not supported for this command yet.");
                return;
            }

            var channelName = command.Args[0];
            var channel = Client.GetChannelByName(channelName);
            if (channel == null)
            {
                await message.RespondAsync($"Failed to lookup channel {channelName}.");
                return;
            }

            var msg = command.Args[1];
            await channel.SendMessageAsync(msg);
        }
    }
}