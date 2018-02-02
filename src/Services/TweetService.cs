namespace BrockBot.Services
{
    using System;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using DSharpPlus;

    using Stream = Tweetinvi.Stream;
    using Tweetinvi;
    using Tweetinvi.Models;
    using Tweetinvi.Streaming;
    using Tweetinvi.Streaming.Parameters;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    public class TweetService
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;
        private readonly IFilteredStream _twitterStream;
        private readonly Timer _timer;

        #endregion

        #region Constructor

        public TweetService(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;

            _timer = new Timer { Interval = FilterBot.OneMinute };
            _timer.Elapsed += MinuteTimerEventHandler;

            var creds = SetCredentials(_config.TwitterUpdates);

            _twitterStream = Stream.CreateFilteredStream(creds);
            _twitterStream.Credentials = creds;
            _twitterStream.StallWarnings = true;
            _twitterStream.FilterLevel = StreamFilterLevel.None;
            _twitterStream.StreamStarted += (sender, e) => _logger.Debug("Successfully started.");
            _twitterStream.StreamStopped += (sender, e) => _logger.Debug($"Stream stopped.\r\n{e.Exception}\r\n{e.DisconnectMessage}");
            _twitterStream.DisconnectMessageReceived += (sender, e) => _logger.Debug($"Disconnected.\r\n{e.DisconnectMessage}");
            _twitterStream.WarningFallingBehindDetected += (sender, e) => _logger.Debug($"Warning Falling Behind Detected: {e.WarningMessage}");

            CheckTwitterFollows();
        }

        #endregion

        #region Public Methods

        public async Task Start()
        {
            if (_twitterStream != null)
            {
                await _twitterStream.StartStreamMatchingAllConditionsAsync();
            }

            if (!_timer.Enabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_twitterStream != null)
            {
                _twitterStream.StopStream();
            }

            if (_timer.Enabled)
            {
                _timer.Stop();
            }
        }

        #endregion

        #region Private Methods

        private TwitterCredentials SetCredentials(TwitterUpdatesConfig config)
        {
            var creds = new TwitterCredentials(config.ConsumerKey, config.ConsumerSecret, config.AccessToken, config.AccessTokenSecret);
            if (creds == null)
            {
                _logger.Error($"Failed to create TwitterCredentials object.");
                return null;
            }

            Auth.SetCredentials(creds);
            return creds;
        }

        private void CheckTwitterFollows()
        {
            if (_twitterStream == null) return;

            foreach (var user in _config.TwitterUpdates.TwitterUsers)
            {
                var userId = Convert.ToInt64(user);
                if (_twitterStream.ContainsFollow(userId)) continue;

#pragma warning disable RECS0165
                _twitterStream.AddFollow(userId, async x =>
#pragma warning restore RECS0165
                {
                    if (userId != x.CreatedBy.Id) return;
                    //if (x.IsRetweet) return;

                    await SendTwitterNotification(x.CreatedBy.Id, x.Url);
                });
            }
        }

        private async Task SendTwitterNotification(long ownerId, string url)
        {
            if (!_config.TwitterUpdates.PostTwitterUpdates) return;

            Console.WriteLine($"Tweet [Owner={ownerId}, Url={url}]");
            await _client.SendMessage(_config.TwitterUpdates.UpdatesChannelWebHook, url);
        }

        private async void MinuteTimerEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckTwitterFollows();

            if (_twitterStream == null) return;
            switch (_twitterStream.StreamState)
            {
                case StreamState.Running:
                    break;
                case StreamState.Pause:
                    _twitterStream.ResumeStream();
                    break;
                case StreamState.Stop:
                    await _twitterStream.StartStreamMatchingAllConditionsAsync();
                    break;
            }
        }

        #endregion
    }
}