namespace BrockBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using BrockBot.Commands;
    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Services;
    using BrockBot.Services.RaidLobby;
    using BrockBot.Utilities;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    //TODO: Add rate limiter to prevent spam.
    //TODO: Notify via SMS or Email or Twilio or w/e.
    //TODO: Keep track of supporters, have a command to check if a paypal email etc or username rather has donated.
    //TODO: Fix new geofence lookup, Upland picked up as Montclair.

    /**PokeAlarm Alternative Logic
     *******************************
     * Dictionary<string[city], List<AlarmObject>> to contain the feed alarm settings.
     * AlarmObject should contain the filter settings such as alarm name, filter checks, geofence file or object, webhook, and anything else that's needed.
     * Upon receiving scanner data check what geofence or city the coordinates are in.
     * After checking where it is, check the associated filters for that AlarmObject.
     * If a filter hits send the notification to the discord webhook.
     */

    public class AlarmObject
    {
        public string GeofenceFile { get; set; }

        public string Webhook { get; set; }
    }

    public class FilterBot
    {
        #region Constants

        public const string BotName = "Brock";
        public const int OneSecond = 1000;
        public const int OneMinute = OneSecond * 60;
        public const int OneHour = OneMinute * 60;
        public const string UnauthorizedAttemptsFileName = "unauthorized_attempts.txt";
        public const string DefaultCrashMessage = "I JUST CRASHED!";

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly Database _db;
        private readonly IEventLogger _logger;
        private readonly Random _rand;
        private Timer _timer;
        private bool _isConnected;

        private readonly ReminderService _reminderSvc;
        private readonly NotificationProcessor _notificationProcessor;
        private AdvertisementService _advertSvc;
        private TweetService _tweetSvc;
        private FeedMonitorService _feedSvc;
        private readonly RaidLobbyManager _lobbyManager;
        private readonly IWeatherService _weatherSvc;

        #endregion

        #region Properties

        public CommandList Commands { get; }

        #endregion

        #region Constructor

        public FilterBot(IEventLogger logger)
        {
            Commands = new CommandList();

            _logger = logger;
            _db = Database.Load();
            if (_db == null)
            {
                _logger.Error("Failed to load database, exiting...");
                Environment.Exit(-1);
            }

            _config = File.Exists(Config.ConfigFilePath) ? Config.Load() : Config.CreateDefaultConfig(true);
            _rand = new Random();

            _client = new DiscordClient
            (
                new DiscordConfiguration
                {
                    AutoReconnect = true,
                    LogLevel = LogLevel.Error,
                    UseInternalLogHandler = true,
                    Token = _config.AuthToken,
                    TokenType = TokenType.Bot
                }
            );

            _client.MessageCreated += Client_MessageCreated;
            _client.Ready += Client_Ready;
            _client.DmChannelCreated += Client_DmChannelCreated;
            _client.GuildMemberAdded += Client_GuildMemberAdded;
            _client.GuildMemberRemoved += Client_GuildMemberRemoved;
            _client.GuildBanAdded += Client_GuildBanAdded;
            _client.GuildBanRemoved += Client_GuildBanRemoved;
            _client.MessageReactionAdded += Client_MessageReactionAdded;
            _client.MessageReactionRemoved += Client_MessageReactionRemoved;

            _reminderSvc = new ReminderService(_client, _db, _logger);
            _notificationProcessor = new NotificationProcessor(_client, _db, _config, _logger);
            _lobbyManager = new RaidLobbyManager(_client, _config, _logger);
            _weatherSvc = new WeatherService(_config.AccuWeatherApiKey, _logger);
        }

        #endregion

        #region Http Server Events

        private void PokemonReceived(object sender, PokemonReceivedEventArgs e)
        {
            if (!_isConnected) return;
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await _notificationProcessor.ProcessPokemon(e.Pokemon)) { IsBackground = true }.Start();
#pragma warning restore RECS0165
        }

        private void RaidReceived(object sender, RaidReceivedEventArgs e)
        {
            if (!_isConnected) return;
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await _notificationProcessor.ProcessRaid(e.Raid)) { IsBackground = true }.Start();
#pragma warning restore RECS0165
        }

        #endregion

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            _logger.Trace($"FilterBot::Client_Ready [{e.Client.CurrentUser.Username}]");

            await _client.UpdateStatusAsync(new DiscordGame($"v{AssemblyUtils.AssemblyVersion}"));

            _isConnected = true;

            if (_config.SendStartupMessage)
            {
                await SendStartupMessage();
            }

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }
            
            if (_advertSvc == null)
            {
                _advertSvc = new AdvertisementService(_client, _config, _logger);
                await _advertSvc.Start();
            }

            if (_feedSvc == null)
            {
                _feedSvc = new FeedMonitorService(_client, _config, _logger);
                _feedSvc.Start();
            }

            try
            {
                if (_tweetSvc == null)
                {
                    _tweetSvc = new TweetService(_client, _config, _logger);
                    _tweetSvc.Start();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Message.Author.IsBot)
            {
                _logger.Trace($"FilterBot::Client_MessageCreated [Username={e.Message.Author.Username} Message={e.Message.Content}]");
            }

            if (e.Message.Author.Id == _client.CurrentUser.Id) return;

            if (e.Message.Author.IsBot)
            {
                await CheckSponsoredRaids(e.Message);
                return;
            }

            if (e.Message.Channel.Id == _config.GiveawayChannelId)
            {
                var command = new Command(_config.CommandsPrefix, e.Message.Content);
                if (!command.ValidCommand && !e.Message.Author.IsBot) return;

                foreach (var giveaway in _config.Giveaways)
                {
                    if (!giveaway.Started) continue;
                    if (giveaway.Winner > 0) continue;

                    var pokemon = _db.Pokemon[giveaway.PokemonId.ToString()].Name;
                    if (string.Compare(e.Message.Content, pokemon, true) != 0) continue;

                    var giveawaysChannel = await _client.GetChannel(_config.AdminCommandsChannelId);
                    if (giveawaysChannel == null)
                    {
                        _logger.Error($"Failed to get giveaways channel with id {0}.");
                        return;
                    }

                    giveaway.Winner = e.Message.Author.Id;

                    var owner = await _client.GetUser(_config.OwnerId);
                    if (owner == null)
                    {
                        _logger.Error($"Failed to find owner with id {_config.OwnerId}.");
                        continue;
                    }

                    await e.Message.RespondAsync($"{pokemon} was correct {e.Message.Author.Mention}! Congratulations you've won a month of supporter status! I will send {owner.Mention} a DM regarding it right now.\r\nThank you all who participated, more giveaways to come.");

                    await _client.SendDirectMessage(owner, $"{e.Message.Author.Mention} guessed correctly with {pokemon} in the giveaway.", null);
                    await giveawaysChannel.GrantPermissions(e.Guild.EveryoneRole, Permissions.None, Permissions.ReadMessageHistory | Permissions.SendMessages);


                    _config.Supporters.Add(e.Message.Author.Id, new Data.Models.Donator { DateDonated = DateTime.Now });

                    //TODO: Add support for supporters list that is checked by Brock, set/remove role etc.
                }
            }
            if (e.Message.Channel.Id == _config.CommandsChannelId ||
                     e.Message.Channel.Id == _config.AdminCommandsChannelId)// ||
                     //_db.Lobbies.Exists(x => string.Compare(x.LobbyName, e.Message.Channel.Name, true) == 0))
            {
                await ParseCommand(e.Message);
            }
        }

        private async Task Client_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            //_logger.Trace($"FilterBot::Client_DmChannelCreated [{e.Channel.Name}]");

            var msg = await e.Channel.GetMessageAsync(e.Channel.LastMessageId);
            if (msg == null)
            {
                _logger.Error($"Failed to find last direct message from id {e.Channel.LastMessageId}.");
                return;
            }

            await ParseCommand(msg);
        }

        private async Task Client_GuildBanAdded(GuildBanAddEventArgs e)
        {
            _logger.Trace($"FilterBot::Client_GuildBanAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = await _client.GetChannel(_config.CommandsChannelId);
            if (channel == null)
            {
                _logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
                return;
            }

            await channel.SendMessageAsync($"OH SNAP! The ban hammer was just dropped on {e.Member.Mention}, cya!");
        }

        private async Task Client_GuildBanRemoved(GuildBanRemoveEventArgs e)
        {
            _logger.Trace($"FilterBot::Client_GuildBanRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = await _client.GetChannel(_config.CommandsChannelId);
            if (channel == null)
            {
                _logger.Error($"Failed to find channel {_config.CommandsChannelId}.");
                return;
            }

            await channel.SendMessageAsync($"Zeus was feeling nice today and unbanned {e.Member.Mention}, welcome back! Hopefully you'll learn to behave this time around. ;)");
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            _logger.Trace($"FilterBot::Client_GuildMemberAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyNewMemberJoined)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null)
                {
                    _logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
                    return;
                }

                await channel.SendMessageAsync($"Everyone let's welcome {e.Member.Mention} to the server! We've been waiting for you!");
            }

            if (_config.AssignNewMembersCityRoles)
            {
                _client.AssignMemberRoles(e.Member, _config.CityRoles);
            }

            if (_config.SendWelcomeMessage)
            {
                await _client.SendWelcomeMessage(e.Member, _config.WelcomeMessage);
            }
        }

        private async Task Client_GuildMemberRemoved(GuildMemberRemoveEventArgs e)
        {
            _logger.Trace($"FilterBot::Client_GuildMemberRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyMemberLeft)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null)
                {
                    _logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
                    return;
                }
                await channel.SendMessageAsync($"Sorry to see you go {e.Member.Mention}, hope to see you back soon!");
            }
        }

        private async Task Client_MessageReactionAdded(MessageReactionAddEventArgs e)
        {
            #region
            //if (!_lobbyManager.ValidRaidEmojis.Contains(e.Emoji.Name)) return;

            //if (e.Channel.Guild == null)
            //{
            //    if (e.User.IsBot) return;

            //    var hasPrivilege = await _client.IsSupporterOrHigher(e.User.Id, _config);
            //    if (!hasPrivilege)
            //    {
            //        await e.Message.RespondAsync($"{e.User.Username} does not have the supporter role assigned.");
            //        return;
            //    }

            //    var origMessageId = Convert.ToUInt64(Utils.GetBetween(e.Message.Content, "#", "#"));
            //    var lobby = GetLobby(e.Channel, ref origMessageId);

            //    var settings = await GetRaidLobbySettings(lobby, origMessageId, e.Message, e.Channel);
            //    if (settings == null)
            //    {
            //        Logger.Error($"Failed to find raid lobby settings for original raid message id {origMessageId}.");
            //        return;
            //    }

            //    await e.Message.DeleteReactionAsync(e.Emoji, e.User);

            //    var lobMessage = default(DiscordMessage);
            //    var embedMsg = settings.RaidMessage?.Embeds[0];

            //    switch (e.Emoji.Name)
            //    {
            //        //case "1⃣":
            //        //    break;
            //        //case "2⃣":
            //        //    break;
            //        //case "3⃣":
            //        //    break;
            //        //case "4⃣":
            //        //    break;
            //        case "5⃣":
            //            lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Five;
            //            lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
            //            lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
            //            await e.Message.DeleteAllReactionsAsync();
            //            break;
            //        case "🔟":
            //            lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Ten;
            //            lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
            //            lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
            //            await e.Message.DeleteAllReactionsAsync();
            //            break;
            //        case "❌":
            //            if (!lobby.UsersComing.ContainsKey(e.User.Id))
            //            {
            //                lobby.UsersComing.Remove(e.User.Id);
            //                lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
            //            }
            //            break;
            //    }
            //    _config.RaidLobbies.ActiveLobbies[origMessageId] = lobby;
            //    _config.Save();
            //}
            //else
            //{
            //    var result = _config.SponsoredRaids.Exists(x => x.ChannelPool.Contains(e.Channel.Id)) ||
            //                 _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id;

            //    if (!result) return;
            //    if (e.User.IsBot) return;

            //    var hasPrivilege = await _client.IsSupporterOrHigher(e.User.Id, _config);
            //    if (!hasPrivilege)
            //    {
            //        await e.Message.RespondAsync($"{e.User.Username} does not have the supporter role assigned.");
            //        return;
            //    }

            //    var originalMessageId = e.Message.Id;
            //    var lobby = GetLobby(e.Channel, ref originalMessageId);

            //    var settings = await GetRaidLobbySettings(lobby, e.Message.Id, e.Message, e.Channel);
            //    if (settings == null)
            //    {
            //        Logger.Error($"Failed to find raid lobby settings for original raid message id {originalMessageId}.");
            //        return;
            //    }

            //    #region
            //    //var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
            //    //if (raidLobbyChannel == null)
            //    //{
            //    //    Logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbies.RaidLobbiesChannelId}.");
            //    //    return;
            //    //}

            //    //RaidLobby lobby = null;
            //    //var originalMessageId = e.Message.Id;
            //    //if (e.Channel.Id == _config.RaidLobbies.RaidLobbiesChannelId)
            //    //{
            //    //    foreach (var item in _config.RaidLobbies.ActiveLobbies)
            //    //    {
            //    //        if (item.Value.LobbyMessageId == e.Message.Id)
            //    //        {
            //    //            originalMessageId = item.Value.OriginalRaidMessageId;
            //    //            lobby = item.Value;
            //    //            break;
            //    //        }
            //    //    }
            //    //}
            //    //else
            //    //{
            //    //    if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
            //    //    {
            //    //        lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
            //    //    }
            //    //    else
            //    //    {
            //    //        lobby = new RaidLobby { OriginalRaidMessageId = originalMessageId, OriginalRaidMessageChannelId = e.Channel.Id };
            //    //        _config.RaidLobbies.ActiveLobbies.Add(originalMessageId, lobby);
            //    //    }
            //    //}

            //    //if (lobby == null)
            //    //{
            //    //    Logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {e.Message.Id}...");
            //    //    await e.Message.DeleteAsync("Raid lobby does not exist anymore.");
            //    //    return;
            //    //}

            //    //var channel = await _client.GetChannel(lobby.OriginalRaidMessageChannelId);
            //    //if (channel == null)
            //    //{
            //    //    Logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
            //    //    return;
            //    //}

            //    //var raidMessage = await channel.GetMessage(originalMessageId);
            //    //if (raidMessage == null)
            //    //{
            //    //    Logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
            //    //    raidMessage = await GetRaidMessage(_config.SponsoredRaids, originalMessageId);
            //    //}
            //    #endregion

            //    await e.Message.DeleteAllReactionsAsync();

            //    var lobbyMessage = default(DiscordMessage);
            //    var embed = settings.RaidMessage?.Embeds[0];

            //    switch (e.Emoji.Name)
            //    {
            //        case "➡":
            //            #region Coming
            //            if (!lobby.UsersComing.ContainsKey(e.User.Id))
            //            {
            //                lobby.UsersComing.Add(e.User.Id, new RaidLobbyUser { Id = e.User.Id, Eta = RaidLobbyEta.NotSet, Players = 1 });
            //            }

            //            if (lobby.UsersReady.ContainsKey(e.User.Id))
            //            {
            //                lobby.UsersReady.Remove(e.User.Id);
            //            }

            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetAccountsReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message
            //            );
            //            break;
            //            #endregion
            //        case "✅":
            //            #region Ready
            //            if (!lobby.UsersReady.ContainsKey(e.User.Id))
            //            {
            //                var players = lobby.UsersComing.ContainsKey(e.User.Id) ? lobby.UsersComing[e.User.Id].Players : 1;
            //                lobby.UsersReady.Add(e.User.Id, new RaidLobbyUser { Id = e.User.Id, Eta = RaidLobbyEta.Here, Players = players });
            //            }

            //            if (lobby.UsersComing.ContainsKey(e.User.Id))
            //            {
            //                lobby.UsersComing.Remove(e.User.Id);
            //            }

            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            if (_config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id)
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //            }
            //            else
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //                await _client.SetDefaultRaidReactions(e.Message, false);
            //            }
            //            break;
            //            #endregion
            //        case "❌":
            //            #region Remove User From Lobby
            //            if (lobby.UsersComing.ContainsKey(e.User.Id)) lobby.UsersComing.Remove(e.User.Id);
            //            if (lobby.UsersReady.ContainsKey(e.User.Id)) lobby.UsersReady.Remove(e.User.Id);

            //            if (lobby.UsersComing.Count == 0 && lobby.UsersReady.Count == 0)
            //            {
            //                lobbyMessage = await settings.RaidLobbyChannel.GetMessage(lobby.LobbyMessageId);
            //                if (lobbyMessage != null)
            //                {
            //                    await lobbyMessage.DeleteAsync();
            //                    lobbyMessage = null;
            //                }

            //                _config.RaidLobbies.ActiveLobbies.Remove(lobby.OriginalRaidMessageId);
            //            }
            //            break;
            //            #endregion
            //        case "1⃣":
            //            #region 1 Account
            //            lobby.UsersComing[e.User.Id].Players = 1;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetEtaReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message
            //            );
            //            break;
            //        #endregion
            //        case "2⃣":
            //            #region 2 Accounts
            //            lobby.UsersComing[e.User.Id].Players = 2;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetEtaReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message
            //            );
            //            break;
            //        #endregion
            //        case "3⃣":
            //            #region 3 Accounts
            //            lobby.UsersComing[e.User.Id].Players = 3;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetEtaReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message
            //            );
            //            break;
            //        #endregion
            //        case "4⃣":
            //            #region 4 Accounts
            //            lobby.UsersComing[e.User.Id].Players = 4;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetEtaReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message
            //            );
            //            break;
            //        #endregion
            //        case "5⃣":
            //            #region 5mins ETA
            //            lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Five;
            //            lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            if (_config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id)
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //            }
            //            else
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //                await _client.SetDefaultRaidReactions(e.Message, false);
            //            }
            //            break;
            //        #endregion
            //        case "🔟":
            //            #region 10mins ETA
            //            lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Ten;
            //            lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            if (_config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id)
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //            }
            //            else
            //            {
            //                await _client.SetDefaultRaidReactions(lobbyMessage, true);
            //                await _client.SetDefaultRaidReactions(e.Message, false);
            //            }
            //            break;
            //        #endregion
            //        case "🔄":
            //            #region Refresh
            //            lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
            //            await _client.SetDefaultRaidReactions
            //            (
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //                ? lobbyMessage
            //                : e.Message,
            //                _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
            //            );
            //            break;
            //            #endregion
            //    }
            //    if (lobby != null)
            //    {
            //        if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
            //        {
            //            _config.RaidLobbies.ActiveLobbies[originalMessageId] = lobby;
            //        }
            //    }
            //    _config.Save();
            //}
            #endregion

            await _lobbyManager.ProcessReaction(e);
        }

        private async Task Client_MessageReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region Public Methods

        public async Task StartAsync()
        {
            _logger.Trace($"FilterBot::Start");

            if (_client == null)
            {
                _logger.Error($"Really don't know how this happened?");
                return;
            }

            var http = new HttpServer(_config, _logger);
            http.PokemonReceived += PokemonReceived;
            http.RaidReceived += RaidReceived;

            if (_timer == null)
            {
                _timer = new Timer(OneMinute);
                _timer.Elapsed += MinuteTimerEventHandler;
                _timer.Start();
            }

            _logger.Info("Connecting to discord server...");

            await Utils.Wait(500);
            await _client.ConnectAsync();
        }

        public async Task StopAsync()
        {
            _logger.Trace($"FilterBot::Stop");

            if (_client == null)
            {
                _logger.Warn($"{AssemblyUtils.AssemblyName} has not been started, therefore it cannot be stopped.");
                return;
            }

            _logger.Info($"Shutting down {AssemblyUtils.AssemblyName}...");

            if (_client != null)
            {
                await _client.DisconnectAsync();

                _client.Dispose();
                //_client = null;
            }
        }

        public bool RegisterCommand<T>(params object[] optionalParameters)
        {
            _logger.Trace($"FilterBot::RegisterCommand [Type={typeof(T).FullName}, OptionalParameters={string.Join(", ", optionalParameters)}]");

            try
            {
                var type = typeof(T);
                var args = new List<object>();
                var constructorInfo = type.GetConstructors()[0];
                var parameters = constructorInfo.GetParameters();

                foreach (var pi in parameters)
                {
                    if (typeof(DiscordClient) == pi.ParameterType)
                        args.Add(_client);
                    else if (typeof(IDatabase) == pi.ParameterType)
                        args.Add(_db);
                    else if (typeof(Config) == pi.ParameterType)
                        args.Add(_config);
                    else if (typeof(ReminderService) == pi.ParameterType)
                        args.Add(_reminderSvc);
                    else if (typeof(IWeatherService) == pi.ParameterType)
                        args.Add(_weatherSvc);
                    else if (typeof(IEventLogger) == pi.ParameterType)
                        args.Add(_logger);
                    else
                    {
                        foreach (var obj in optionalParameters)
                        {
                            if (obj.GetType() == pi.ParameterType)
                            {
                                args.Add(obj);
                            }
                        }
                    }
                }

                var attributes = type.GetCustomAttributes(typeof(CommandAttribute), false);
                var attr = new CommandAttribute();
                if (attributes.Length > 0)
                {
                    attr = attributes[0] as CommandAttribute;
                }

                var command = (ICustomCommand)Activator.CreateInstance(type, args.ToArray());
                //foreach (Type t in type.GetInterfaces())
                //{
                //    if (typeof(IApp) == t)
                //        data.ClientHandlers.App = (IApp)objectValue;
                //    else if (typeof(IUI) == t)
                //        data.ClientHandlers.UI = (IUI)objectValue;
                //}

                var cmds = attr.CommandNames.ToArray();

                if (!Commands.ContainsKey(cmds) && !Commands.ContainsValue(command))
                {
                    Commands.Add(cmds, command);
                    _logger.Info($"Command{(cmds.Length > 1 ? "s" : null)} {string.Join(", ", cmds)} registered.");

                    return true;
                }

                _logger.Error($"Failed to register command{(cmds.Length > 1 ? "s" : null)} {string.Join(", ", cmds)}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
        }

        public void UnregisterCommand(string[] cmdNames)
        {
            _logger.Trace($"FilterBot::UnregisterCommand [CommandNames={string.Join(", ", cmdNames)}]");

            if (!Commands.ContainsKey(cmdNames))
            {
                _logger.Error($"Failed to unregister command {string.Join(", ", cmdNames)} because it is not currently registered.");
                return;
            }

            if (!Commands.Remove(cmdNames))
            {
                _logger.Error($"Failed to unregister command {string.Join(", ", cmdNames)}");
                return;
            }
        }

        public async Task AlertOwnerOfCrash()
        {
            _logger.Trace($"FilterBot::AlertOwnerOfCrash");

            var owner = await _client.GetUser(_config.OwnerId);
            if (owner == null)
            {
                _logger.Error($"Failed to find owner with owner id {_config.OwnerId}...");
                return;
            }

            await _client.SendDirectMessage(owner, DefaultCrashMessage, null);
        }

        #endregion

        #region Private Methods

        private async Task CheckSponsoredRaids(DiscordMessage message)
        {
            foreach (var sponsored in _config.SponsoredRaids)
            {
                if (!sponsored.ChannelPool.Contains(message.ChannelId)) continue;

                await _client.SetDefaultRaidReactions(message, false);

                foreach (var embed in message.Embeds)
                {
                    foreach (var keyword in sponsored.Keywords)
                    {
                        if (embed.Description.Contains(keyword))
                        {
                            await _client.SendMessage(sponsored.WebHook, string.Empty, embed);
                        }
                    }
                }
            }
        }

        private async Task SendStartupMessage()
        {
            _logger.Trace($"FilterBot::SendStartupMessage");

            var randomWelcomeMessage = _config.StartupMessages[_rand.Next(0, _config.StartupMessages.Count - 1)];
            await _client.SendMessage(_config.StartupMessageWebHook, randomWelcomeMessage);
        }

        private async void MinuteTimerEventHandler(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_db.LastChecked.Day != DateTime.Now.Day)
            {
                await _notificationProcessor.WriteStatistics();

                _db.LastChecked = DateTime.Now;
                _db.Subscriptions.ForEach(x => x.ResetNotifications());
                _db.Save();
            }

            if (_client == null) return;

            if (_lobbyManager == null) return;

            try
            {
                var keys = new ulong[_config.RaidLobbies.ActiveLobbies.Keys.Count];
                _config.RaidLobbies.ActiveLobbies.Keys.CopyTo(keys, 0);

                for (int i = 0; i < keys.Length; ++i)
                {
                    var lobby = _config.RaidLobbies.ActiveLobbies[keys[i]];
                    if (!lobby.IsExpired) continue;

                    if (_config.RaidLobbies.ActiveLobbies.ContainsKey(keys[i]))
                    {
                        if (!await _lobbyManager.DeleteExpiredRaidLobby(keys[i]))
                        {
                            _logger.Error($"Failed to delete raid lobby message with id {keys[i]}.");
                        }

                        _config.RaidLobbies.ActiveLobbies.Remove(keys[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            #region Old Raid Lobby System
            // if (_client == null) return;
            // try
            // {
            //     foreach (var lobby in _db.Lobbies)
            //     {
            //         if (lobby.IsExpired)
            //         {
            //             var channel = await _client.GetChannel(lobby.ChannelId);
            //             if (channel == null)
            //             {
            //                 Logger.Error($"Failed to delete expired raid lobby channel because channel {lobby.LobbyName} ({lobby.ChannelId}) does not exist.");
            //                 continue;
            //             }
            //             //await channel.DeleteAsync($"Raid lobby {lobby.LobbyName} ({lobby.ChannelId}) no longer needed.");
            //         }
            //         await _client.UpdateLobbyStatus(lobby);
            //     }

            //     //_db.Servers.ForEach(server => server.Lobbies.RemoveAll(lobby => lobby.IsExpired));
            //     _db.Lobbies.RemoveAll(lobby => lobby.IsExpired);
            // }
            //#pragma warning disable RECS0022
            // catch { }
            //#pragma warning restore RECS0022
            #endregion
        }

        private async Task CheckSupporterStatus(ulong guildId)
        {
            if (!_client.Guilds.ContainsKey(guildId)) return;

            foreach (var member in _client.Guilds[guildId].Members)
            {
                if (member.HasSupporterRole(_config.SupporterRoleId))
                {
                    if (await _client.IsSupporterStatusExpired(_config, member.Id))
                    {
                        _logger.Debug($"Removing supporter role from user {member.Id} because their time has expired...");

                        //if (_db.Subscriptions.Exists(x => x.UserId == member.Id))
                        //{
                        //    _db.Subscriptions.Find(x => x.UserId == member.Id).Enabled = false;
                        //    _db.Save();

                        //    Logger.Debug($"Disabled Pokemon and Raid notifications for user {member.Username} ({member.Id}).");
                        //}

                        //if (!await _client.RemoveRole(member.Id, guildId, _config.SupporterRoleId))
                        //{
                        //    Logger.Error($"Failed to remove supporter role from user {member.Id}.");
                        //    continue;
                        //}

                        _logger.Debug($"Successfully removed supporter role from user {member.Id}.");
                    }
                }
            }
        }

        private async Task ParseCommand(DiscordMessage message)
        {
            if (string.IsNullOrEmpty(message.Content)) return;

            _logger.Trace($"FilterBot::ParseCommand [Message={message.Content}]");

            var command = new Command(_config.CommandsPrefix, message.Content);
            if (!command.ValidCommand && !message.Author.IsBot) return;

            if (Commands.ContainsKey(command.Name))
            {
                if (command.Name.ToLower() == "help" ||
                    command.Name.ToLower() == "commands")
                {
                    await ParseHelpCommand(message, command);
                    return;
                }

                var isOwner = message.Author.Id.IsAdmin(_config);
                switch (Commands[command.Name].PermissionLevel)
                {
                    case CommandPermissionLevel.Admin:
                        if (!isOwner)
                        {
                            message.Author.LogUnauthorizedAccess(command.FullCommand, UnauthorizedAttemptsFileName);
                            await message.RespondAsync($"{message.Author.Mention} is not authorized to execute these type of commands, your unique user id has been logged.");
                            return;
                        }
                        break;
                    case CommandPermissionLevel.Moderator:
                        var isModerator = message.Author.Id.IsModeratorOrHigher(_config);
                        if (!isModerator)
                        {
                            await message.RespondAsync($"Command `{_config.CommandsPrefix}{command.Name}` is only available to Moderators.");
                            return;
                        }
                        break;
                    case CommandPermissionLevel.Supporter:
                        var isSupporter = await _client.IsSupporterOrHigher(message.Author.Id, _config);
                        if (!isSupporter)
                        {
                            await message.RespondAsync($"Command `{_config.CommandsPrefix}{command.Name}` is only available to Supporters.");
                            return;
                        }
                        break;
                }

                await Commands[command.Name].Execute(message, command);
                //TODO: If admin only command, check if channel is admin command channel.

                _db.Save();
            }
            else if (_config.CustomCommands.ContainsKey(command.Name))
            {
                var isSupporter = await _client.IsSupporterOrHigher(message.Author.Id, _config);
                if (!isSupporter)
                {
                    await message.RespondAsync($"{message.Author.Mention} Only supporters have access to custom commands, please consider donating in order to unlock this feature.");
                    return;
                }

                await message.RespondAsync(_config.CustomCommands[command.Name]);
            }
        }

        private async Task ParseHelpCommand(DiscordMessage message, Command command)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Help Command Information");

            var isOwner = message.Author.Id.IsAdmin(_config);
            var categories = Commands.GetCommandsByCategory();
            if (command.HasArgs && command.Args.Count == 1)
            {
                var category = Commands.ParseCategory(command.Args[0]);
                if (string.IsNullOrEmpty(category))
                {
                    await message.RespondAsync($"{message.Author.Mention} has entered an invalid help command category.");
                    return;
                }

                eb.AddField(category, "-");
                foreach (var cmd in categories[category])
                {
                    if (cmd.PermissionLevel == CommandPermissionLevel.Admin && !isOwner) continue;
                    if (cmd.PermissionLevel == CommandPermissionLevel.Moderator && !message.Author.Id.IsModerator(_config)) continue;
                    //if (cmd.PermissionLevel == CommandPermissionLevel.Supporter && !await _client.IsSupporterOrHigher(message.Author.Id, _config)) continue;

                    //TODO: Sort by index or something.
                    var attr = cmd.GetType().GetAttribute<CommandAttribute>();
                    eb.AddField
                    (
                        _config.CommandsPrefix + string.Join(", " + _config.CommandsPrefix, attr.CommandNames),
                        attr.Description + "\r\n" + attr.Example
                    );
                }
            }
            else
            {
                foreach (var category in categories)
                {
                    //if (category.Value.Exists(x => x.PermissionLevel == CommandPermissionLevel.Admin))
                    //{
                    //    if (!isOwner) continue;
                    //}

                    //if (category.Value.Exists(x => x.PermissionLevel == CommandPermissionLevel.Moderator))
                    //{
                    //    if (!message.Author.Id.IsModerator(_config)) continue;
                    //}

                    //var isSupporterOrHigher = await _client.IsSupporterOrHigher(message.Author.Id, _config);
                    //if (category.Value.Exists(x => x.PermissionLevel == CommandPermissionLevel.Supporter)) if (!isSupporterOrHigher) continue;

                    eb.AddField(category.Key, $"{_config.CommandsPrefix}help {category.Key.ToLower().Replace(" ", "")}");
                }
            }

            eb.WithFooter($"Developed by versx, Version {AssemblyUtils.AssemblyVersion}");

            if (eb.Fields.Count == 0) return;

            var embed = eb.Build();
            if (embed == null) return;

            await message.RespondAsync(message.Author.Mention, false, embed);
        }

        #endregion

        //#region Raid Lobby

        //private RaidLobby GetLobby(DiscordChannel channel, ref ulong originalMessageId)
        //{
        //    RaidLobby lobby = null;
        //    if (channel.Id == _config.RaidLobbies.RaidLobbiesChannelId)
        //    {
        //        foreach (var item in _config.RaidLobbies.ActiveLobbies)
        //        {
        //            if (item.Value.LobbyMessageId == originalMessageId)
        //            {
        //                originalMessageId = item.Value.OriginalRaidMessageId;
        //                lobby = item.Value;
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
        //        {
        //            lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
        //        }
        //        else
        //        {
        //            lobby = new RaidLobby { OriginalRaidMessageId = originalMessageId, OriginalRaidMessageChannelId = channel.Id, Started = DateTime.Now };
        //            _config.RaidLobbies.ActiveLobbies.Add(originalMessageId, lobby);
        //        }
        //    }

        //    return lobby;
        //}

        //private async Task<bool> DeleteExpiredRaidLobby(ulong originalMessageId)
        //{
        //    Logger.Trace($"FilterBot::DeleteExpiredRaidLobby [OriginalMessageId={originalMessageId}]");

        //    if (!_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId)) return false;

        //    var lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
        //    var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
        //    if (raidLobbyChannel == null)
        //    {
        //        Logger.Error($"Failed to find raid lobby channel with id {_config.RaidLobbies.RaidLobbiesChannelId}, does it exist?");
        //        return false;
        //    }

        //    var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
        //    if (lobbyMessage == null)
        //    {
        //        Logger.Error($"Failed to find raid lobby message with id {lobby.LobbyMessageId}, must have already been deleted.");
        //        return true;
        //    }

        //    try
        //    {
        //        await lobbyMessage.DeleteAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //    }

        //    return false;
        //}

        //private async Task<RaidLobbySettings> GetRaidLobbySettings(RaidLobby lobby, ulong originalMessageId, DiscordMessage message, DiscordChannel channel)
        //{
        //    Logger.Trace($"FilterBot::GetRaidLobbySettings [OriginalMessageId={originalMessageId}, DiscordMessage={message.Content}, DiscordChannel={channel.Name}]");

        //    var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
        //    if (raidLobbyChannel == null)
        //    {
        //        Logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbies.RaidLobbiesChannelId}.");
        //        return null;
        //    }

        //    if (lobby == null)
        //    {
        //        Logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {message.Id}...");
        //        await message.DeleteAsync("Raid lobby does not exist anymore.");
        //        return null;
        //    }

        //    var origChannel = await _client.GetChannel(lobby.OriginalRaidMessageChannelId);
        //    if (origChannel == null)
        //    {
        //        Logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
        //        return null;
        //    }

        //    var raidMessage = await origChannel.GetMessage(originalMessageId);
        //    if (raidMessage == null)
        //    {
        //        Logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
        //        raidMessage = await GetRaidMessage(_config.SponsoredRaids, originalMessageId);
        //    }

        //    _config.Save();

        //    return new RaidLobbySettings
        //    {
        //        //Lobby = lobby,
        //        OriginalRaidMessageChannel = origChannel,
        //        RaidMessage = raidMessage,
        //        RaidLobbyChannel = raidLobbyChannel
        //    };
        //}

        //private async Task<DiscordMessage> UpdateRaidLobbyMessage(RaidLobby lobby, DiscordChannel raidLobbyChannel, DiscordEmbed raidMessage)
        //{
        //    Logger.Trace($"FilterBot::UpdateRaidLobbyMessage [RaidLobby={lobby.LobbyMessageId}, DiscordChannel={raidLobbyChannel.Name}, DiscordMessage={raidMessage.Title}]");

        //    var coming = await GetUsernames(lobby.UsersComing);
        //    var ready = await GetUsernames(lobby.UsersReady);

        //    var msg = $"**Trainers on the way:**{Environment.NewLine}```{string.Join(Environment.NewLine, coming)}  ```{Environment.NewLine}**Trainers at the raid:**{Environment.NewLine}```{string.Join(Environment.NewLine, ready)}  ```";
        //    var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
        //    if (lobbyMessage != null)
        //    {
        //        await lobbyMessage.DeleteAsync();
        //        lobbyMessage = null;
        //    }

        //    if (lobbyMessage == null)
        //    {
        //        lobbyMessage = await raidLobbyChannel.SendMessageAsync(msg, false, raidMessage);
        //        lobby.LobbyMessageId = lobbyMessage.Id;
        //    }
        //    _config.Save();

        //    if (lobbyMessage == null)
        //    {
        //        Logger.Error($"Failed to set default raid reactions to message {lobby.LobbyMessageId}, couldn't find message...");
        //        return null;
        //    }

        //    lobby.LobbyMessageId = lobbyMessage.Id;
        //    return lobbyMessage;
        //}

        //private uint TimeLeft(DateTime etaStart)
        //{
        //    try
        //    {
        //        return Convert.ToUInt32((etaStart.AddMinutes(5) - DateTime.Now).Minutes);
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.Error(ex);
        //        return 0;
        //    }
        //}

        //private async Task<DiscordMessage> GetRaidMessage(List<SponsoredRaidsConfig> sponsoredRaids, ulong messageId)
        //{
        //    Logger.Trace($"FilterBot::GetRaidMessage [SponsoredRaids={sponsoredRaids.Count}, MessageId={messageId}]");

        //    foreach (var sponsored in sponsoredRaids)
        //    {
        //        foreach (var channelId in sponsored.ChannelPool)
        //        {
        //            var channel = await _client.GetChannel(channelId);
        //            if (channel == null)
        //            {
        //                Logger.Error($"Failed to find channel {channelId}.");
        //                continue;
        //            }

        //            var message = await channel.GetMessage(messageId);
        //            if (message == null) continue;

        //            return message;
        //        }
        //    }

        //    return null;
        //}

        //private async Task<List<string>> GetUsernames(Dictionary<ulong, RaidLobbyUser> users)
        //{
        //    var list = new List<string>();
        //    foreach (var item in users)
        //    {
        //        var user = await _client.GetUser(item.Key);
        //        if (user == null)
        //        {
        //            Logger.Error($"Failed to find discord user with id {item.Key}.");
        //            continue;
        //        }

        //        //TODO: Fix Eta countdown.
        //        var timeLeft = item.Value.Eta;// TimeLeft(item.Value.EtaStart);
        //        //if (timeLeft == 0)
        //        //{
        //        //    //User is late, send DM.
        //        //    var dm = await _client.SendDirectMessage(user, $"{user.Mention} you're late for the raid, do you want to extend your time? If not please click the red cross button below to remove yourself from the raid lobby.\r\n#{item.Key}#", null);
        //        //    if (dm == null)
        //        //    {
        //        //        Logger.Error($"Failed to send {user.Username} a direct message letting them know they are late for the raid.");
        //        //        continue;
        //        //    }

        //        //    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":five:"));
        //        //    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":keycap_ten:"));
        //        //    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":x:"));
        //        //}

        //        //var eta = (item.Value.Eta != RaidLobbyEta.Here && item.Value.Eta != RaidLobbyEta.NotSet ? $"{timeLeft} minute{(timeLeft == 1 ? null : "s")}" : item.Value.Eta.ToString());
        //        var eta = (item.Value.Eta != RaidLobbyEta.Here && item.Value.Eta != RaidLobbyEta.NotSet ? $"{timeLeft} minute" : item.Value.Eta.ToString());
        //        list.Add($"{user.Username} ({item.Value.Players} account{(item.Value.Players == 1 ? "" : "s")}, ETA: {eta})");
        //    }
        //    return list;
        //}

        //#endregion
    }

    public class Helpers
    {
        private readonly Database _db;

        private static double[] cpMultipliers =
        {
            0.094, 0.16639787, 0.21573247, 0.25572005, 0.29024988,
            0.3210876, 0.34921268, 0.37523559, 0.39956728, 0.42250001,
            0.44310755, 0.46279839, 0.48168495, 0.49985844, 0.51739395,
            0.53435433, 0.55079269, 0.56675452, 0.58227891, 0.59740001,
            0.61215729, 0.62656713, 0.64065295, 0.65443563, 0.667934,
            0.68116492, 0.69414365, 0.70688421, 0.71939909, 0.7317,
            0.73776948, 0.74378943, 0.74976104, 0.75568551, 0.76156384,
            0.76739717, 0.7731865, 0.77893275, 0.78463697, 0.79030001
        };

        public Helpers(Database db)
        {
            _db = db;
        }

        public static uint PokemonIdFromName(IDatabase db, string name)
        {
            foreach (var p in db.Pokemon)
            {
                if (p.Value.Name.ToLower().Contains(name.ToLower()))
                {
                    return Convert.ToUInt32(p.Key);
                }
            }

            return 0;
        }

        public static string GetPokemonGender(PokemonGender gender)
        {
            switch (gender)
            {
                case PokemonGender.Male:
                    return "\u2642";
                case PokemonGender.Female:
                    return "\u2640";
                default:
                    return "?";

            }
        }

        public string GetSize(IDatabase db, int id, float height, float weight)
        {
            if (!db.Pokemon.ContainsKey(id.ToString())) return string.Empty;

            var stats = db.Pokemon[id.ToString()];
            float weightRatio = weight / (float)stats.BaseStats.Weight;
            float heightRatio = height / (float)stats.BaseStats.Height;
            float size = heightRatio + weightRatio;

            if (size < 1.5) return "tiny";
            if (size <= 1.75) return "small";
            if (size < 2.25) return "normal";
            if (size <= 2.5) return "large";
            return "big";
        }

        public int MaxCpAtLevel(int id, int level)
        {
            double multiplier = cpMultipliers[level - 1];
            double attack = (BaseAtk(id) + 15) * multiplier;
            double defense = (BaseDef(id) + 15) * multiplier;
            double stamina = (BaseSta(id) + 15) * multiplier;
            return (int)Math.Max(10, Math.Floor(Math.Sqrt(attack * attack * defense * stamina) / 10));
        }

        public int GetLevel(double cpModifier)
        {
            double unRoundedLevel;

            if (cpModifier < 0.734)
            {
                unRoundedLevel = (58.35178527 * cpModifier * cpModifier - 2.838007664 * cpModifier + 0.8539209906);
            }
            else
            {
                unRoundedLevel = 171.0112688 * cpModifier - 95.20425243;
            }

            return (int)Math.Round(unRoundedLevel);
        }

        public int GetRaidBossCp(int bossId, int raidLevel)
        {
            int stamina = 600;

            switch (raidLevel)
            {
                case 1:
                    stamina = 600;
                    break;
                case 2:
                    stamina = 1800;
                    break;
                case 3:
                    stamina = 3000;
                    break;
                case 4:
                    stamina = 7500;
                    break;
                case 5:
                    stamina = 12500;
                    break;
            }
            return (int)Math.Floor(((BaseAtk(bossId) + 15) * Math.Sqrt(BaseDef(bossId) + 15) * Math.Sqrt(stamina)) / 10);
        }

        private double BaseAtk(int id)
        {
            if (!_db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = _db.Pokemon[id.ToString()];

            return stats.BaseStats.Attack;
        }

        private double BaseDef(int id)
        {
            if (!_db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = _db.Pokemon[id.ToString()];

            return stats.BaseStats.Defense;
        }

        private double BaseSta(int id)
        {
            if (!_db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = _db.Pokemon[id.ToString()];

            return stats.BaseStats.Stamina;
        }

        public static List<string> GetStrengths(string type)
        {
            var types = new string[0];
            switch (type.ToLower())
            {
                case "normal":
                    break;
                case "fighting":
                    types = new string[] { "Normal", "Rock", "Steel", "Ice", "Dark" };
                    break;
                case "flying":
                    types = new string[] { "Fighting", "Bug", "Grass" };
                    break;
                case "poison":
                    types = new string[] { "Grass", "Fairy" };
                    break;
                case "ground":
                    types = new string[] { "Poison", "Rock", "Steel", "Fire", "Electric" };
                    break;
                case "rock":
                    types = new string[] { "Flying", "Bug", "Fire", "Ice" };
                    break;
                case "bug":
                    types = new string[] { "Grass", "Psychic", "Dark" };
                    break;
                case "ghost":
                    types = new string[] { "Ghost", "Psychic" };
                    break;
                case "steel":
                    types = new string[] { "Rock", "Ice" };
                    break;
                case "fire":
                    types = new string[] { "Bug", "Steel", "Grass", "Ice" };
                    break;
                case "water":
                    types = new string[] { "Ground", "Rock", "Fire" };
                    break;
                case "grass":
                    types = new string[] { "Ground", "Rock", "Water" };
                    break;
                case "electric":
                    types = new string[] { "Flying", "Water" };
                    break;
                case "psychic":
                    types = new string[] { "Fighting", "Poison" };
                    break;
                case "ice":
                    types = new string[] { "Flying", "Ground", "Grass", "Dragon" };
                    break;
                case "dragon":
                    types = new string[] { "Dragon" };
                    break;
                case "dark":
                    types = new string[] { "Ghost", "Psychic" };
                    break;
                case "fairy":
                    types = new string[] { "Fighting", "Dragon", "Dark" };
                    break;
            }
            return new List<string>(types);
        }

        public static List<string> GetWeaknesses(string type)
        {
            var types = new string[0];
            switch (type.ToLower())
            {
                case "normal":
                    types = new string[] { "Fighting" };
                    break;
                case "fighting":
                    types = new string[] { "Flying", "Psychic", "Fairy" };
                    break;
                case "flying":
                    types = new string[] { "Rock", "Electric", "Ice" };
                    break;
                case "poison":
                    types = new string[] { "Ground", "Psychic" };
                    break;
                case "ground":
                    types = new string[] { "Water", "Grass", "Ice" };
                    break;
                case "rock":
                    types = new string[] { "Fighting", "Ground", "Steel", "Water", "Grass" };
                    break;
                case "bug":
                    types = new string[] { "Flying", "Rock", "Fire" };
                    break;
                case "ghost":
                    types = new string[] { "Ghost", "Dark" };
                    break;
                case "steel":
                    types = new string[] { "Fighting", "Ground", "Fire" };
                    break;
                case "fire":
                    types = new string[] { "Ground", "Rock", "Water" };
                    break;
                case "water":
                    types = new string[] { "Grass", "Electric" };
                    break;
                case "grass":
                    types = new string[] { "Flying", "Poison", "Bug", "Fire", "Ice" };
                    break;
                case "electric":
                    types = new string[] { "Ground" };
                    break;
                case "psychic":
                    types = new string[] { "Bug", "Ghost", "Dark" };
                    break;
                case "ice":
                    types = new string[] { "Fighting", "Rock", "Steel", "Fire" };
                    break;
                case "dragon":
                    types = new string[] { "Ice", "Dragon", "Fairy" };
                    break;
                case "dark":
                    types = new string[] { "Fighting", "Bug", "Fairy" };
                    break;
                case "fairy":
                    types = new string[] { "Poison", "Steel" };
                    break;
            }
            return new List<string>(types);
        }
    }
}