namespace BrockBot.Services
{
    using System;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using DSharpPlus;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    public class AdvertisementService
    {
        #region Constants

        public const string DefaultAdvertisementMessage = ":arrows_counterclockwise: Welcome to **{server}**'s server, to assign yourself to a city feed or team please review the pinned messages in the {bot} channel as well as read the messages in the {faq} channel.";
        //public const int DefaultAdvertisementMessageDifference = 10;
        public const int MaxAdvertisementWaitTimeoutMinutes = 30;

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;
        private readonly Timer _adTimer;

        #endregion

        #region Constructor

        public AdvertisementService(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;

            _adTimer = new Timer { Interval = _config.Advertisement.PostInterval * FilterBot.OneMinute };
            _adTimer.Elapsed += AdvertisementTimerEventHandler;
        }

        #endregion

        #region Public Methods

        public async Task Start()
        {
            _logger.Trace($"AdvertisementService::Start");

            await Init();

            if (!_adTimer.Enabled)
            {
                _adTimer.Start();
                AdvertisementTimerEventHandler(this, null);
            }
        }

        public void Stop()
        {
            _logger.Trace($"AdvertisementService::Stop");

            if (_adTimer.Enabled)
            {
                _adTimer.Stop();
            }
        }

        #endregion

        #region Private Methods

        private async Task Init()
        {
            var channel = await _client.GetChannel(_config.CommandsChannelId);
            if (channel == null) return;
        }

        private async void AdvertisementTimerEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            //await CheckFeedStatus();

            //await CheckSupporterStatus(_client.Guilds[0].Id);

            await PostAdvertisement();
        }

        private async Task PostAdvertisement()
        {
            if (!_config.Advertisement.Enabled) return;
            if (_config.Advertisement.ChannelId == 0) return;

            _logger.Trace($"AdvertisementService::PostAdvertisement");

            try
            {
                var advertisementChannel = await _client.GetChannel(_config.Advertisement.ChannelId);
                if (advertisementChannel == null)
                {
                    _logger.Error($"Failed to retrieve advertisement channel with id {_config.Advertisement.ChannelId}.");
                    return;
                }

                var cmdChannel = await _client.GetChannel(_config.CommandsChannelId);
                if (cmdChannel == null)
                {
                    _logger.Error($"Failed to retrieve commands channel with id {_config.CommandsChannelId}.");
                    return;
                }

                var faqChannel = await _client.GetChannel(392736371284115456); //TODO: Add #faq channel id to config.
                if (faqChannel == null)
                {
                    _logger.Error($"Failed to retrieve frequently asked questions channel.");
                    //return;
                }

                if (_config.Advertisement.LastMessageId == 0)
                {
                    var msg = (string.IsNullOrEmpty(_config.Advertisement.Message)
                        ? DefaultAdvertisementMessage
                        : _config.Advertisement.Message)
                        .Replace("{server}", advertisementChannel.Guild.Name)
                        .Replace("{bot}", cmdChannel.Mention)
                        .Replace("{faq}", faqChannel.Mention);
                    var sentMessage = await advertisementChannel.SendMessageAsync(msg);
                    _config.Advertisement.LastMessageId = sentMessage.Id;
                    _config.Save();
                    return;
                }

                var messages = await advertisementChannel.GetMessagesAsync();
                if (messages == null)
                {
                    _logger.Error($"Failed to retrieve the list of messages from the advertisement channel {advertisementChannel.Name}.");
                    return;
                }

                var lastBotMessageIndex = -1;
                for (int i = 0; i < messages.Count; i++)
                {
                    if (messages[i].Id == _config.Advertisement.LastMessageId && messages[i].Author.IsBot)
                    {
                        lastBotMessageIndex = i;
                    }
                }

                if (lastBotMessageIndex > _config.Advertisement.MessageThreshold || lastBotMessageIndex == -1)
                {
                    var guild = await _client.GetGuildAsync(advertisementChannel.GuildId);
                    if (guild == null)
                    {
                        _logger.Error($"Failed to retrieve guild from channel guild id.");
                        return;
                    }

                    var latestMessage = await advertisementChannel.GetMessage(advertisementChannel.LastMessageId);
                    if (latestMessage == null)
                    {
                        _logger.Error($"Failed to retrieve the latest message from the advertisement channel {advertisementChannel.Name} with message id {advertisementChannel.LastMessageId}.");
                        return;
                    }

                    //Check if it's been at least 30 minutes since someone wrote a message in order to not be intrusive.
                    var ts = DateTime.Now.Subtract(new DateTime(latestMessage.Timestamp.Ticks));
                    var canPost = ts.Minutes >= MaxAdvertisementWaitTimeoutMinutes;

                    if (!canPost) return;

                    var message = await advertisementChannel.GetMessage(_config.Advertisement.LastMessageId);
                    if (message != null)
                    {
                        await message.DeleteAsync();
                    }

                    var msg = (string.IsNullOrEmpty(_config.Advertisement.Message)
                        ? DefaultAdvertisementMessage
                        : _config.Advertisement.Message)
                        .Replace("{server}", advertisementChannel.Guild.Name)
                        .Replace("{bot}", cmdChannel.Mention);
                    var sentMessage = await advertisementChannel.SendMessageAsync(msg);
                    _config.Advertisement.LastMessageId = sentMessage.Id;
                    _config.Save();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        #endregion
    }
}