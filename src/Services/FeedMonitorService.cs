namespace BrockBot.Services
{
    using System;
    using System.Threading.Tasks;
    using System.Timers;

    using DSharpPlus;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    public class FeedMonitorService
    {
        #region Constants

        public const int FeedDownThreshold = 15;

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;
        private readonly Timer _timer;

        #endregion

        #region Constructor

        public FeedMonitorService(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;

            _timer = new Timer { Interval = 15 * (1000 * 60) };
            _timer.Elapsed += CheckFeedEventHandler;
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        #endregion

        #region Private Methods

        private async void CheckFeedEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_config.FeedStatus.Enabled) return;
            if (_config.FeedStatus.Channels.Count == 0) return;

            for (int i = 0; i < _config.FeedStatus.Channels.Count; i++)
            {
                await CheckFeedChannelStatus(_config.FeedStatus.Channels[i]);
            }

            await Utils.Wait(500);
        }

        private async Task CheckFeedChannelStatus(ulong channelId)
        {
            var channel = await _client.GetChannel(channelId);
            if (channel == null)
            {
                _logger.Error($"Failed to find Discord channel with id {channelId}.");
                return;
            }

            var mostRecent = await channel.GetMessage(channel.LastMessageId);
            if (mostRecent == null)
            {
                _logger.Error($"Failed to retrieve last message for channel {channel.Name}.");
                return;
            }

            if (IsFeedUp(mostRecent.CreationTimestamp.DateTime, FeedDownThreshold))
                return;

            var owner = await _client.GetUser(_config.OwnerId);
            if (owner == null)
            {
                _logger.Error($"Failed to find owner with id {_config.OwnerId}.");
                return;
            }

            await _client.SendDirectMessage(owner, $"DISCORD FEED **{channel.Name}** IS DOWN!", null);
            await Utils.Wait(200);
        }

        private bool IsFeedUp(DateTime created, int thresholdMinutes = 15)
        {
            var now = DateTime.Now;
            var diff = now.Subtract(created);
            var isUp = diff.Minutes < thresholdMinutes;
            return isUp;
        }

        #endregion
    }
}