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

        public static string DefaultAdvertisementMessage;
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
            //DefaultAdvertisementMessage = $":arrows_counterclockwise: Welcome to **{(channel.Guild == null ? "versx" : channel.Guild.Name)}**'s server! To assign or unassign yourself to or from a city feed or team please review the pinned messages in the {channel.Mention} channel or type `.help`. Please also read the #faq channel if you have any questions, otherwise post them.";
            //":arrows_counterclockwise: Welcome to versx's server, in order to see a city feed you will need to assign yourself to a city role using the .feed command followed by one or more of the available cities separated by a comma (,): {0}, or None.";

            DefaultAdvertisementMessage = @"Hello {username}, welcome to **versx**'s server!
My name is Brock and I'm here to assist you with certain things. Most commands that you'll send me will need to be sent to the #bot channel in the server but I can also process some commands via direct message.

First things first you might want to set your team, there are three available teams: Valor, Mystic, and Instinct. To set your team you'll want to use the `.team Valor/Mystic/Instinct` command, although this is optional. For more details please read the pinned message in the #bot channel titled Team Assignment.
Next you'll need to assign youself to some city feeds to see Pokemon spawns and Raids. Quickest way is to type the `.feedme all` command, otherwise please read the pinned message in the #bot channel titled City Feeds for more details.
Lastly if you'd like to get direct messages from me when a certain Pokemon with a specific IV percentage or raid appears, to do so please read the pinned message in the #bot channel titled Pokemon Notifications.

I will only send you direct message notifications of Pokemon or raids for city feeds that you are assigned to.
**To see a full list of my available commands please send me a direct message containing `.help`.**

Once you've completed the above steps you'll be all set to go catch those elusive monsters, be safe and have fun!";
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

                if (_config.Advertisement.LastMessageId == 0)
                {
                    var msg = (string.IsNullOrEmpty(_config.Advertisement.Message)
                        ? DefaultAdvertisementMessage
                        : _config.Advertisement.Message)
                        .Replace("{server}", advertisementChannel.Guild.Name)
                        .Replace("{bot}", cmdChannel.Mention);
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
                    var canPost = false;
                    var ts = DateTime.Now.Subtract(new DateTime(latestMessage.Timestamp.Ticks));
                    if (ts.Minutes >= MaxAdvertisementWaitTimeoutMinutes) canPost = true;

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