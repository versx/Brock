namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Services;

    [Command(
        Categories.General,
        "Starts a giveaway at the specified time.",
        "\tExample: `.giveaway 4:30pm`",
        "giveaway"
    )]
    public class GiveawayCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public GiveawayCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (message.Author.Id != _config.OwnerId) return;

            var startTime = command.Args[0];

            var giveawayChannel = await _client.GetChannel(_config.GiveawayChannelId);
            if (giveawayChannel == null)
            {
                _logger.Error($"Failed to get giveaways channel with id {0}.");
                return;
            }

            await giveawayChannel.LockChannel(message.Channel.Guild.EveryoneRole);

            var giveSvc = new GiveawayService(_client, _config.GiveawayChannelId, _config);
            giveSvc.Start(DateTime.Parse(startTime));
        }
    }
}