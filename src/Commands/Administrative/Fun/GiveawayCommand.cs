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
        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        #endregion

        #region Constructor

        public GiveawayCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (message.Author.Id != _config.OwnerId) return;

            var startTime = command.Args[0];
            if (!DateTime.TryParse(startTime, out DateTime time))
            {
                await message.RespondAsync($"{message.Author.Mention} you've entered an incorrect time format.");
                return;
            }

            var giveawayChannel = await _client.GetChannel(_config.GiveawayChannelId);
            if (giveawayChannel == null)
            {
                _logger.Error($"Failed to get giveaways channel with id {_config.GiveawayChannelId}.");
                return;
            }

            //TODO: Fix lock channel.
            await giveawayChannel.LockChannel(message.Channel.Guild.EveryoneRole);

            var giveSvc = new GiveawayService(_client, _config.GiveawayChannelId, _config);
            if (command.Args.Count == 2)
            {
                var pokemon = command.Args[1];
                if (uint.TryParse(pokemon, out uint pokeId))
                {
                    giveSvc.Start(time, pokeId);
                }
            }
            else
            {
                giveSvc.Start(time);
            }

            await message.RespondAsync($"{message.Author.Mention} next giveaway will start at {time}...");
        }

        #endregion
    }
}