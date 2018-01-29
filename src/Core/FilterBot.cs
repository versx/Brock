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
    using BrockBot.Data.Models;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Services;
    using BrockBot.Services.RaidLobby;
    using BrockBot.Utilities;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using Stream = Tweetinvi.Stream;
    using Tweetinvi;
    using Tweetinvi.Models;
    using Tweetinvi.Streaming;
    using Tweetinvi.Streaming.Parameters;

    //TODO: Add rate limiter to prevent spam.
    //TODO: Notify via SMS or Email or Twilio or w/e.
    //TODO: Add support for pokedex # or name for Pokemon and Raid subscriptions.
    //TODO: Keep track of supporters, have a command to check if a paypal email etc or username rather has donated.
    //TODO: Add response messages to invalid commands/arguments provided.

    //TODO: Parse feed cities with spaces, or replace all feeds with a single command?

    public class FilterBot
    {
        #region Constants

        public const string BotName = "Brock";
        public static string DefaultAdvertisementMessage;
        public const int DefaultAdvertisementMessageDifference = 10;
        public const int OneSecond = 1000;
        public const int OneMinute = OneSecond * 60;
        public const int OneHour = OneMinute * 60;
        public const string UnauthorizedAttemptsFileName = "unauthorized_attempts.txt";

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly Database _db;
        private readonly Random _rand;
        private Timer _timer;
        private Timer _adTimer;
        private IFilteredStream _twitterStream;

        private readonly ReminderService _reminderSvc;
        private readonly NotificationProcessor _notificationProcessor;

        #endregion

        #region Properties

        public IEventLogger Logger { get; set; }

        public CommandList Commands { get; private set; }

        #endregion

        #region Constructor

        public FilterBot(IEventLogger logger)
        {
            Logger = logger;
            Commands = new CommandList();

            _db = Database.Load();
            if (_db == null)
            {
                Utils.LogError(new Exception("Failed to load database, exiting..."));
                Environment.Exit(-1);
            }

            _config = File.Exists(Config.ConfigFilePath) ? Config.Load() : Config.CreateDefaultConfig(true);
            _rand = new Random();

            _client = new DiscordClient
            (
                new DiscordConfiguration
                {
                    AutoReconnect = true,
                    LogLevel = LogLevel.Debug,
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

            _reminderSvc = new ReminderService(_client, _db, Logger);
            _notificationProcessor = new NotificationProcessor(_client, _db, _config, Logger);
        }

        #endregion

        #region Http Server Events

        private void PokemonReceived(object sender, PokemonReceivedEventArgs e)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await _notificationProcessor.ProcessPokemon(e.Pokemon)) { IsBackground = true }.Start();
#pragma warning restore RECS0165
        }

        private void RaidReceived(object sender, RaidReceivedEventArgs e)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await _notificationProcessor.ProcessRaid(e.Raid)) { IsBackground = true }.Start();
#pragma warning restore RECS0165
        }

        #endregion

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_Ready [{e.Client.CurrentUser.Username}]");

            //await DisplaySettings();

            await _client.UpdateStatusAsync(new DiscordGame($"v{AssemblyUtils.AssemblyVersion}"));

            if (_config.SendStartupMessage)
            {
                await SendStartupMessage();
            }

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }

            await Utils.Wait(3 * OneSecond);
            await Init();

            _adTimer = new Timer { Interval = _config.Advertisement.PostInterval * OneMinute };
            _adTimer.Elapsed += AdvertisementTimer_Elapsed;
            _adTimer.Start();
            //AdvertisementTimer_Elapsed(this, null);
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Message.Author.IsBot)
            {
                Logger.Trace($"FilterBot::Client_MessageCreated [Username={e.Message.Author.Username} Message={e.Message.Content}]");
            }

            if (e.Message.Author.Id == _client.CurrentUser.Id) return;

            if (e.Message.Author.IsBot)
            {
                await CheckSponsoredRaids(e.Message);
            }
            else if (e.Message.Channel.Id == _config.CommandsChannelId ||
                     e.Message.Channel.Id == _config.AdminCommandsChannelId)// ||
                     //_db.Lobbies.Exists(x => string.Compare(x.LobbyName, e.Message.Channel.Name, true) == 0))
            {
                await ParseCommand(e.Message);
            }
        }

        private async Task Client_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_DmChannelCreated [{e.Channel.Name}]");

            var msg = await e.Channel.GetMessageAsync(e.Channel.LastMessageId);
            if (msg == null)
            {
                Logger.Error($"Failed to find last direct message from id {e.Channel.LastMessageId}.");
                return;
            }

            await ParseCommand(msg);
        }

        private async Task Client_GuildBanAdded(GuildBanAddEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildBanAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = await _client.GetChannel(_config.CommandsChannelId);
            if (channel == null)
            {
                Logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
                return;
            }

            await channel.SendMessageAsync($"OH SNAP! The ban hammer was just dropped on {e.Member.Mention}, cya!");
        }

        private async Task Client_GuildBanRemoved(GuildBanRemoveEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildBanRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = await _client.GetChannel(_config.CommandsChannelId);
            if (channel == null)
            {
                Logger.Error($"Failed to find channel {_config.CommandsChannelId}.");
                return;
            }

            await channel.SendMessageAsync($"Zeus was feeling nice today and unbanned {e.Member.Mention}, welcome back! Hopefully you'll learn to behave this time around. ;)");
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildMemberAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyNewMemberJoined)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null)
                {
                    Logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
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
            Logger.Trace($"FilterBot::Client_GuildMemberRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyMemberLeft)
            {
                var channel = await _client.GetChannel(_config.CommandsChannelId);
                if (channel == null)
                {
                    Logger.Error($"Failed to find channel with id {_config.CommandsChannelId}.");
                    return;
                }
                await channel.SendMessageAsync($"Sorry to see you go {e.Member.Mention}, hope to see you back soon!");
            }
        }

        private async Task Client_MessageReactionAdded(MessageReactionAddEventArgs e)
        {
            var isSupporter = await _client.HasSupporterRole(e.User.Id, _config.SupporterRoleId);
            var isOwner = e.Message.Author.Id == _config.OwnerId;
            if (!(isSupporter || isOwner))
            {
                await e.Message.RespondAsync($"{e.User.Username} does not have the supporter role assigned.");
                return;
            }

            if (e.Message.Channel.Guild == null)
            {
                var origMessageId = Convert.ToUInt64(Utils.GetBetween(e.Message.Content, "#", "#"));
                var settings = await GetRaidLobbySettings(origMessageId, e.Message, e.Channel);

                await e.Message.DeleteReactionAsync(e.Emoji, e.User);

                var lobMessage = default(DiscordMessage);
                var embedMsg = settings.RaidMessage?.Embeds[0];
                if (settings == null)
                {
                    Logger.Error($"Failed to find raid lobby settings for original raid message id {origMessageId}.");
                    return;
                }

                switch (e.Emoji.Name)
                {
                    //case "1⃣":
                    //    break;
                    //case "2⃣":
                    //    break;
                    //case "3⃣":
                    //    break;
                    //case "4⃣":
                    //    break;
                    case "5⃣":
                        settings.Lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Five;
                        settings.Lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
                        lobMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embedMsg);
                        await e.Message.DeleteAllReactionsAsync();
                        break;
                    case "🔟":
                        settings.Lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Ten;
                        settings.Lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
                        lobMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embedMsg);
                        await e.Message.DeleteAllReactionsAsync();
                        break;
                    case "❌":
                        if (!settings.Lobby.UsersComing.ContainsKey(e.User.Id))
                        {
                            settings.Lobby.UsersComing.Remove(e.User.Id);
                            lobMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embedMsg);
                        }
                        break;
                }
            }
            else
            {
                var result = _config.SponsoredRaids.Exists(x => x.ChannelPool.Contains(e.Channel.Id)) ||
                             _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id;

                if (!result) return;
                if (e.User.IsBot) return;

                var settings = await GetRaidLobbySettings(e.Message.Id, e.Message, e.Channel);

                #region
                //var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
                //if (raidLobbyChannel == null)
                //{
                //    Logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbies.RaidLobbiesChannelId}.");
                //    return;
                //}

                //RaidLobby lobby = null;
                //var originalMessageId = e.Message.Id;
                //if (e.Channel.Id == _config.RaidLobbies.RaidLobbiesChannelId)
                //{
                //    foreach (var item in _config.RaidLobbies.ActiveLobbies)
                //    {
                //        if (item.Value.LobbyMessageId == e.Message.Id)
                //        {
                //            originalMessageId = item.Value.OriginalRaidMessageId;
                //            lobby = item.Value;
                //            break;
                //        }
                //    }
                //}
                //else
                //{
                //    if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
                //    {
                //        lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
                //    }
                //    else
                //    {
                //        lobby = new RaidLobby { OriginalRaidMessageId = originalMessageId, OriginalRaidMessageChannelId = e.Channel.Id };
                //        _config.RaidLobbies.ActiveLobbies.Add(originalMessageId, lobby);
                //    }
                //}

                //if (lobby == null)
                //{
                //    Logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {e.Message.Id}...");
                //    await e.Message.DeleteAsync("Raid lobby does not exist anymore.");
                //    return;
                //}

                //var channel = await _client.GetChannel(lobby.OriginalRaidMessageChannelId);
                //if (channel == null)
                //{
                //    Logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
                //    return;
                //}

                //var raidMessage = await channel.GetMessage(originalMessageId);
                //if (raidMessage == null)
                //{
                //    Logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
                //    raidMessage = await GetRaidMessage(_config.SponsoredRaids, originalMessageId);
                //}
                #endregion

                await e.Message.DeleteAllReactionsAsync();

                var lobbyMessage = default(DiscordMessage);
                var embed = settings.RaidMessage?.Embeds[0];

                switch (e.Emoji.Name)
                {
                    case "➡":
                        #region Coming
                        if (!settings.Lobby.UsersComing.ContainsKey(e.User.Id))
                        {
                            settings.Lobby.UsersComing.Add(e.User.Id, new RaidLobbyUser { Id = e.User.Id, Eta = RaidLobbyEta.NotSet, Players = 1 });
                        }

                        if (settings.Lobby.UsersReady.ContainsKey(e.User.Id))
                        {
                            settings.Lobby.UsersReady.Remove(e.User.Id);
                        }

                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetAccountsReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message
                        );
                        break;
                    #endregion
                    case "✅":
                        #region Ready
                        if (settings.Lobby.UsersComing.ContainsKey(e.User.Id))
                        {
                            settings.Lobby.UsersComing.Remove(e.User.Id);
                        }

                        if (!settings.Lobby.UsersReady.ContainsKey(e.User.Id))
                        {
                            settings.Lobby.UsersReady.Add(e.User.Id, new RaidLobbyUser { Id = e.User.Id, Eta = RaidLobbyEta.Here, Players = 1 });
                        }

                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetDefaultRaidReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message,
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                        );
                        break;
                    #endregion
                    case "❌":
                        #region Close Lobby
                        lobbyMessage = await settings.RaidLobbyChannel.GetMessage(settings.Lobby.LobbyMessageId);
                        if (lobbyMessage != null)
                        {
                            await lobbyMessage.DeleteAsync();
                            lobbyMessage = null;
                        }

                        _config.RaidLobbies.ActiveLobbies.Remove(settings.Lobby.OriginalRaidMessageId);
                        _config.Save();
                        break;
                    #endregion
                    case "1⃣":
                        #region 1 Account
                        settings.Lobby.UsersComing[e.User.Id].Players = 1;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetEtaReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message
                        );
                        break;
                    #endregion
                    case "2⃣":
                        #region 2 Accounts
                        settings.Lobby.UsersComing[e.User.Id].Players = 2;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetEtaReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message
                        );
                        break;
                    #endregion
                    case "3⃣":
                        #region 3 Accounts
                        settings.Lobby.UsersComing[e.User.Id].Players = 3;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetEtaReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message
                        );
                        break;
                    #endregion
                    case "4⃣":
                        #region 4 Accounts
                        settings.Lobby.UsersComing[e.User.Id].Players = 4;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetEtaReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message
                        );
                        break;
                    #endregion
                    case "5⃣":
                        #region 5mins ETA
                        settings.Lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Five;
                        settings.Lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        if (_config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id)
                        {
                            await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        }
                        else
                        {
                            await _client.SetDefaultRaidReactions(lobbyMessage, true);
                            await _client.SetDefaultRaidReactions(e.Message, false);
                        }
                        break;
                    #endregion
                    case "🔟":
                        #region 10mins ETA
                        settings.Lobby.UsersComing[e.User.Id].Eta = RaidLobbyEta.Ten;
                        settings.Lobby.UsersComing[e.User.Id].EtaStart = DateTime.Now;
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        if (_config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id)
                        {
                            await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        }
                        else
                        {
                            await _client.SetDefaultRaidReactions(lobbyMessage, true);
                            await _client.SetDefaultRaidReactions(e.Message, false);
                        }
                        break;
                    #endregion
                    case "🔄":
                        #region Refresh
                        lobbyMessage = await UpdateRaidLobbyMessage(settings.Lobby, settings.RaidLobbyChannel, embed);
                        await _client.SetDefaultRaidReactions
                        (
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                            ? lobbyMessage
                            : e.Message,
                            _config.RaidLobbies.RaidLobbiesChannelId == e.Channel.Id
                        );
                        break;
                        #endregion
                }
            }
        }

        private async Task Client_MessageReactionRemoved(MessageReactionRemoveEventArgs e)
        {
            await Task.CompletedTask;
        }

        #endregion

        #region Public Methods

        public async Task StartAsync()
        {
            Logger.Trace($"FilterBot::Start");

            if (_client == null)
            {
                Logger.Error($"Really don't know how this happened?");
                return;
            }

            var http = new HttpServer(_config, Logger);
            http.PokemonReceived += PokemonReceived;
            http.RaidReceived += RaidReceived;

            if (_timer == null)
            {
                _timer = new Timer(60000);
#pragma warning disable RECS0165
                _timer.Elapsed += async (sender, e) =>
#pragma warning restore RECS0165
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

                    if (_client == null) return;

                    try
                    {
                        foreach (var lobby in _config.RaidLobbies.ActiveLobbies)
                        {
                            if (!lobby.Value.IsExpired) continue;

                            if (_config.RaidLobbies.ActiveLobbies.ContainsKey(lobby.Key))
                            {
                                if (!await DeleteExpiredRaidLobby(lobby.Key))
                                {
                                    Logger.Error($"Failed to delete raid lobby message with id {lobby.Key}.");
                                }

                                _config.RaidLobbies.ActiveLobbies.Remove(lobby.Key);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
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
                };
                _timer.Start();
            }

            Logger.Info("Connecting to discord server...");
            await _client.ConnectAsync();

            var creds = new TwitterCredentials(_config.TwitterUpdates.ConsumerKey, _config.TwitterUpdates.ConsumerSecret, _config.TwitterUpdates.AccessToken, _config.TwitterUpdates.AccessTokenSecret);
            Auth.SetCredentials(creds);

            _twitterStream = Stream.CreateFilteredStream(creds);
            _twitterStream.Credentials = creds;
            _twitterStream.StallWarnings = true;
            _twitterStream.FilterLevel = StreamFilterLevel.None;
            _twitterStream.StreamStarted += (sender, e) => Logger.Info("Successfully started.");
            _twitterStream.StreamStopped += (sender, e) => Console.WriteLine($"Stream stopped.\r\n{e.Exception}\r\n{e.DisconnectMessage}");
            _twitterStream.DisconnectMessageReceived += (sender, e) => Console.WriteLine($"Disconnected.\r\n{e.DisconnectMessage}");
            _twitterStream.WarningFallingBehindDetected += (sender, e) => Console.WriteLine($"Warning Falling Behind Detected: {e.WarningMessage}");
            CheckTwitterFollows();
            await _twitterStream.StartStreamMatchingAllConditionsAsync();

            await Task.Delay(-1);
        }

        public async Task StopAsync()
        {
            Logger.Trace($"FilterBot::Stop");

            if (_client == null)
            {
                Logger.Warn($"{AssemblyUtils.AssemblyName} has not been started, therefore it cannot be stopped.");
                return;
            }

            Logger.Info($"Shutting down {AssemblyUtils.AssemblyName}...");

            if (_client != null)
            {
                await _client.DisconnectAsync();

                _client.Dispose();
                //_client = null;
            }
        }

        public bool RegisterCommand<T>(params object[] optionalParameters)
        {
            Logger.Trace($"FilterBot::RegisterCommand [Type={typeof(T).FullName}, OptionalParameters={string.Join(", ", optionalParameters)}]");

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
                    else if (typeof(IEventLogger) == pi.ParameterType)
                        args.Add(Logger);
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
                    Logger.Info($"Command{(cmds.Length > 1 ? "s" : null)} {string.Join(", ", cmds)} registered.");

                    return true;
                }

                Logger.Error($"Failed to register command{(cmds.Length > 1 ? "s" : null)} {string.Join(", ", cmds)}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
        }

        public void UnregisterCommand(string[] cmdNames)
        {
            Logger.Trace($"FilterBot::UnregisterCommand [CommandNames={string.Join(", ", cmdNames)}]");

            if (!Commands.ContainsKey(cmdNames))
            {
                Logger.Error($"Failed to unregister command {string.Join(", ", cmdNames)} because it is not currently registered.");
                return;
            }

            if (!Commands.Remove(cmdNames))
            {
                Logger.Error($"Failed to unregister command {string.Join(", ", cmdNames)}");
                return;
            }
        }

        public async Task AlertOwnerOfCrash()
        {
            var owner = await _client.GetUser(_config.OwnerId);
            if (owner == null)
            {
                Logger.Error($"Failed to find owner with owner id {_config.OwnerId}...");
                return;
            }

            await _client.SendDirectMessage(owner, "I JUST CRASHED!", null);
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
            var randomWelcomeMessage = _config.StartupMessages[_rand.Next(0, _config.StartupMessages.Count - 1)];
            await _client.SendMessage(_config.StartupMessageWebHook, randomWelcomeMessage);
        }

        private async Task DisplaySettings()
        {
            Logger.Trace($"FilterBot::DisplaySettings");

            Console.WriteLine($"********** Current Settings **********");
            var owner = await _client.GetUser(_config.OwnerId);
            Console.WriteLine($"Owner: {owner?.Username} ({_config.OwnerId})");
            Console.WriteLine($"Authentication Token: {_config.AuthToken}");
            Console.WriteLine($"Commands Channel Id: {_config.CommandsChannelId}");
            Console.WriteLine($"Commands Prefix: {_config.CommandsPrefix}");
            Console.WriteLine($"Admin Commands Channel Id: {_config.AdminCommandsChannelId}");
            Console.WriteLine($"Allow Team Assignment: {(_config.AllowTeamAssignment ? "Yes" : "No")}");
            Console.WriteLine($"Team Roles: {string.Join(", ", _config.TeamRoles)}");
            Console.WriteLine($"City Roles: {string.Join(", ", _config.CityRoles)}");
            Console.WriteLine($"Notify New Member Joined: {(_config.NotifyNewMemberJoined ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Left: {(_config.NotifyMemberLeft ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Banned: {(_config.NotifyMemberBanned ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Unbanned: {(_config.NotifyMemberUnbanned ? "Yes" : "No")}");
            Console.WriteLine($"Send Startup Message: {(_config.SendStartupMessage ? "Yes" : "No")}");
            Console.WriteLine($"Startup Messages: {string.Join(", ", _config.StartupMessages)}");
            Console.WriteLine($"Startup Message WebHook: {_config.StartupMessageWebHook}");
            Console.WriteLine($"Send Welcome Message: {_config.SendWelcomeMessage}");
            Console.WriteLine($"Welcome Message: {_config.WelcomeMessage}");
            //Console.WriteLine($"Sponsored Raid Channel Pool: {string.Join(", ", _config.SponsoredRaids.ChannelPool)}");
            //Console.WriteLine($"Sponsored Raid Keywords: {string.Join(", ", _config.SponsoredRaids.Keywords)}");
            //Console.WriteLine($"Sponsored Raids WebHook: {_config.SponsoredRaids.WebHook}");
            Console.WriteLine();
            Console.WriteLine($"Twitter Notification Settings:");
            Console.WriteLine($"Post Twitter Updates: {_config.TwitterUpdates.PostTwitterUpdates}");
            Console.WriteLine($"Consumer Key: {_config.TwitterUpdates.ConsumerKey}");
            Console.WriteLine($"Consumer Secret: {_config.TwitterUpdates.ConsumerSecret}");
            Console.WriteLine($"Access Token: {_config.TwitterUpdates.AccessToken}");
            Console.WriteLine($"Access Token Secret: {_config.TwitterUpdates.AccessTokenSecret}");
            Console.WriteLine($"Twitter Updates Channel WebHook: {_config.TwitterUpdates.UpdatesChannelWebHook}");
            Console.WriteLine($"Subscribed Twitter Users: {string.Join(", ", _config.TwitterUpdates.TwitterUsers)}");
            Console.WriteLine();
            Console.WriteLine($"Custom User Commands:");
            foreach (var cmd in _config.CustomCommands)
            {
                Console.WriteLine($"{cmd.Key}=>{cmd.Value}");
            }
            Console.WriteLine();
            //foreach (var server in _db.Servers)
            //{
            //    Console.WriteLine($"Guild Id: {server.GuildId}");
            Console.WriteLine("Subscriptions:");
            Console.WriteLine();
            foreach (var sub in _db.Subscriptions)
            {
                var user = await _client.GetUser(sub.UserId);
                if (user != null)
                {
                    Console.WriteLine($"Username: {user.Username}, Enabled: {(sub.Enabled ? "Yes" : "No")}");
                    Console.WriteLine($"Pokemon Subscriptions:");
                    foreach (var poke in sub.Pokemon)
                    {
                        if (!_db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;
                        Console.WriteLine(_db.Pokemon[poke.PokemonId.ToString()].Name + $" (Id: {poke.PokemonId}, Minimum CP: {poke.MinimumCP}, Minimum IV: {poke.MinimumIV})");
                    }
                    Console.WriteLine();
                    Console.WriteLine($"Raid Subscriptions:");
                    foreach (var raid in sub.Raids)
                    {
                        if (!_db.Pokemon.ContainsKey(raid.PokemonId.ToString())) continue;
                        Console.WriteLine(_db.Pokemon[raid.PokemonId.ToString()].Name + $" (Id: {raid.PokemonId})");
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
            //Console.WriteLine("Raid Lobbies:");
            //Console.WriteLine();
            //foreach (var lobby in _db.Lobbies)
            //{
            //    Console.WriteLine($"Lobby Name: {lobby.LobbyName}");
            //    Console.WriteLine($"Raid Boss: {lobby.PokemonName}");
            //    Console.WriteLine($"Gym Name: {lobby.GymName}");
            //    Console.WriteLine($"Address: {lobby.Address}");
            //    Console.WriteLine($"Start Time: {lobby.StartTime}");
            //    Console.WriteLine($"Expire Time: {lobby.ExpireTime}");
            //    Console.WriteLine($"Minutes Left: {lobby.MinutesLeft}");
            //    Console.WriteLine($"Is Expired: {lobby.IsExpired}");
            //    Console.WriteLine($"# Users Checked-In: {lobby.NumUsersCheckedIn}");
            //    Console.WriteLine($"# Users On The Way: {lobby.NumUsersOnTheWay}");
            //    Console.WriteLine($"Original Raid Message Id: {lobby.OriginalRaidMessageId}");
            //    Console.WriteLine($"Pinned Raid Message Id{lobby.PinnedRaidMessageId}");
            //    Console.WriteLine($"Channel Id: {lobby.ChannelId}");
            //    Console.WriteLine($"Raid Lobby User List:");
            //    foreach (var lobbyUser in lobby.UserCheckInList)
            //    {
            //        Console.WriteLine($"User Id: {lobbyUser.UserId}");
            //        Console.WriteLine($"Is OnTheWay: {lobbyUser.IsOnTheWay}");
            //        Console.WriteLine($"OnTheWay Time: {lobbyUser.OnTheWayTime}");
            //        Console.WriteLine($"Is Checked-In: {lobbyUser.IsCheckedIn}");
            //        Console.WriteLine($"Check-In Time: {lobbyUser.CheckInTime}");
            //        Console.WriteLine($"User Count: {lobbyUser.UserCount}");
            //        Console.WriteLine($"ETA: {lobbyUser.ETA}");
            //    }
            //    Console.WriteLine();
            //}
            Console.WriteLine();
            Console.WriteLine();
            //}
            Console.WriteLine($"**************************************");
        }

        private async void AdvertisementTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await CheckFeedStatus();

            //await CheckSupporterStatus(_client.Guilds[0].Id);

            await PostAdvertisement();
        }

        private async Task CheckFeedStatus()
        {
            if (!_config.FeedStatus.Enabled) return;
            if (_config.FeedStatus.Channels.Count == 0) return;

            for (int i = 0; i < _config.FeedStatus.Channels.Count; i++)
            {
                var channel = await _client.GetChannel(_config.FeedStatus.Channels[i]);
                if (channel == null)
                {
                    Logger.Error($"Failed to find Discord channel with id {_config.FeedStatus.Channels[i]}.");
                    continue;
                }

                var mostRecent = await channel.GetMessage(channel.LastMessageId);
                if (mostRecent == null)
                {
                    Logger.Error($"Failed to retrieve last message for channel {channel.Name}.");
                    continue;
                }

                if (IsFeedUp(mostRecent.CreationTimestamp.DateTime))
                    continue;

                var owner = await _client.GetUser(_config.OwnerId);
                if (owner == null)
                {
                    Logger.Error($"Failed to find owner with id {_config.OwnerId}.");
                    continue;
                }

                await _client.SendDirectMessage(owner, $"DISCORD FEED **{channel.Name}** IS DOWN!", null);
                await Utils.Wait(200);
            }

            await Utils.Wait(500);
        }

        private bool IsFeedUp(DateTime created, int thresholdMinutes = 15)
        {
            var now = DateTime.Now;
            var diff = now.Subtract(created);
            var isUp = diff.TotalMinutes < thresholdMinutes;
            return isUp;
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
                        Logger.Debug($"Removing supporter role from user {member.Id} because their time has expired...");

                        if (_db.Subscriptions.Exists(x => x.UserId == member.Id))
                        {
                            _db.Subscriptions.Find(x => x.UserId == member.Id).Enabled = false;
                            _db.Save();

                            Logger.Debug($"Disabled Pokemon and Raid notifications for user {member.Username} ({member.Id}).");
                        }

                        //if (!await _client.RemoveRole(member.Id, guildId, _config.SupporterRoleId))
                        //{
                        //    Logger.Error($"Failed to remove supporter role from user {member.Id}.");
                        //    continue;
                        //}

                        Logger.Debug($"Successfully removed supporter role from user {member.Id}.");
                    }
                }
            }
        }

        private async Task PostAdvertisement()
        {
            if (!_config.Advertisement.Enabled) return;
            if (_config.Advertisement.ChannelId == 0) return;

            try
            {
                var advertisementChannel = await _client.GetChannel(_config.Advertisement.ChannelId);
                if (advertisementChannel == null)
                {
                    Logger.Error($"Failed to retrieve advertisement channel with id {_config.Advertisement.ChannelId}.");
                    return;
                }

                var cmdChannel = await _client.GetChannel(_config.CommandsChannelId);
                if (cmdChannel == null)
                {
                    Logger.Error($"Failed to retrieve commands channel with id {_config.CommandsChannelId}.");
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
                    Logger.Error($"Failed to retrieve the list of messages from the advertisement channel {advertisementChannel.Name}.");
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
                        Logger.Error($"Failed to retrieve guild from channel guild id.");
                        return;
                    }

                    var latestMessage = await advertisementChannel.GetMessage(advertisementChannel.LastMessageId);
                    if (latestMessage == null)
                    {
                        Logger.Error($"Failed to retrieve the latest message from the advertisement channel {advertisementChannel.Name} with message id {advertisementChannel.LastMessageId}.");
                        return;
                    }

                    //Check if it's been at least 5 minutes since someone wrote a message in order to not be intrusive.
                    var canPost = false;
                    var ts = DateTime.Now.Subtract(new DateTime(latestMessage.Timestamp.Ticks));
                    if (ts.Minutes > 5) canPost = true;

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
                Logger.Error(ex);
            }
        }

        #endregion

        #region Commands

        private async Task ParseCommand(DiscordMessage message)
        {
            Logger.Trace($"FilterBot::ParseCommand [Message={message.Content}]");

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

                var isOwner = message.Author.Id == _config.OwnerId;
                switch (Commands[command.Name].PermissionLevel)
                {
                    case CommandPermissionLevel.Admin:
                        if (!isOwner)
                        {
                            message.Author.LogUnauthorizedAccess(UnauthorizedAttemptsFileName);
                            await message.RespondAsync($"{message.Author.Mention} is not authorized to execute these type of commands, your unique user id has been logged.");
                            return;
                        }
                        break;
                    case CommandPermissionLevel.Moderator:
                        //var isModerator = await _client.HasModeratorRole(message.Author.Id, _config.ModeratorRoleId);
                        var isModerator = _config.Moderators.Contains(message.Author.Id);
                        if (!(isModerator || isOwner))
                        {
                            await message.RespondAsync($"Command {_config.CommandsPrefix}{command.Name} is only available to Moderators.");
                            return;
                        }
                        break;
                    case CommandPermissionLevel.Supporter:
                        var isSupporter = await _client.HasSupporterRole(message.Author.Id, _config.SupporterRoleId);
                        if (!(isSupporter || isOwner))
                        {
                            await message.RespondAsync($"Command {_config.CommandsPrefix}{command.Name} is only available to Supporters.");
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
                await message.RespondAsync(_config.CustomCommands[command.Name]);
            }
        }

        private async Task ParseHelpCommand(DiscordMessage message, Command command)
        {
            var eb = new DiscordEmbedBuilder();
            eb.WithTitle("Help Command Information");

            var categories = GetCommandsByCategory();
            if (command.HasArgs && command.Args.Count == 1)
            {
                var category = ParseCategory(command.Args[0]);
                if (string.IsNullOrEmpty(category))
                {
                    await message.RespondAsync("You have entered an invalid help command category.");
                    return;
                }

                eb.AddField(category, "-");
                foreach (var cmd in categories[category])
                {
                    var isOwner = message.Author.Id == _config.OwnerId;
                    if (cmd.PermissionLevel == CommandPermissionLevel.Admin && !isOwner) continue;

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
                    eb.AddField(category.Key, $"{_config.CommandsPrefix}help {category.Key.ToLower().Replace(" ", "")}");
                }
            }

            var embed = eb
                .WithFooter($"Developed by versx\r\nVersion {AssemblyUtils.AssemblyVersion}")
                .Build();
            await message.RespondAsync(string.Empty, false, embed);
        }

        private Dictionary<string, List<ICustomCommand>> GetCommandsByCategory()
        {
            var categories = new Dictionary<string, List<ICustomCommand>>();
            foreach (var cmd in Commands)
            {
                var attr = cmd.Value.GetType().GetAttribute<CommandAttribute>();
                if (!categories.ContainsKey(attr.Category))
                {
                    categories.Add(attr.Category, new List<ICustomCommand>());
                }
                categories[attr.Category].Add(cmd.Value);
            }
            return categories;
        }

        private string ParseCategory(string shorthandCategory)
        {
            var helpCategory = shorthandCategory.ToLower();
            foreach (var key in GetCommandsByCategory())
            {
                if (key.Key.ToLower().Replace(" ", "") == helpCategory)
                {
                    helpCategory = key.Key;
                }
            }
            return helpCategory;
        }

        #endregion

        #region Twitter

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

        #endregion

        #region Raid Lobby

        private async Task<bool> DeleteExpiredRaidLobby(ulong originalMessageId)
        {
            if (!_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId)) return false;

            var lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
            var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                Logger.Error($"Failed to find raid lobby channel with id {_config.RaidLobbies.RaidLobbiesChannelId}, does it exist?");
                return false;
            }

            var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
            if (lobbyMessage == null)
            {
                Logger.Error($"Failed to find raid lobby message with id {lobby.LobbyMessageId}, must have already been deleted.");
                return true;
            }

            try
            {
                await lobbyMessage.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
        }

        private async Task<RaidLobbySettings> GetRaidLobbySettings(ulong originalMessageId, DiscordMessage message, DiscordChannel channel)
        {
            RaidLobby lobby = null;
            if (channel.Id == _config.RaidLobbies.RaidLobbiesChannelId)
            {
                foreach (var item in _config.RaidLobbies.ActiveLobbies)
                {
                    if (item.Value.LobbyMessageId == originalMessageId)
                    {
                        originalMessageId = item.Value.OriginalRaidMessageId;
                        lobby = item.Value;
                        break;
                    }
                }
            }
            else
            {
                if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
                {
                    lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
                }
                else
                {
                    lobby = new RaidLobby { OriginalRaidMessageId = originalMessageId, OriginalRaidMessageChannelId = channel.Id, Started = DateTime.Now };
                    _config.RaidLobbies.ActiveLobbies.Add(originalMessageId, lobby);
                }
            }

            var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                Logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbies.RaidLobbiesChannelId}.");
                return null;
            }

            if (lobby == null)
            {
                Logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {message.Id}...");
                await message.DeleteAsync("Raid lobby does not exist anymore.");
                return null;
            }

            var origChannel = await _client.GetChannel(lobby.OriginalRaidMessageChannelId);
            if (origChannel == null)
            {
                Logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
                return null;
            }

            var raidMessage = await origChannel.GetMessage(originalMessageId);
            if (raidMessage == null)
            {
                Logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
                raidMessage = await GetRaidMessage(_config.SponsoredRaids, originalMessageId);
            }

            return new RaidLobbySettings
            {
                Lobby = lobby,
                OriginalRaidMessageChannel = origChannel,
                RaidMessage = raidMessage,
                RaidLobbyChannel = raidLobbyChannel
            };
        }

        private async Task<DiscordMessage> UpdateRaidLobbyMessage(RaidLobby lobby, DiscordChannel raidLobbyChannel, DiscordEmbed raidMessage)
        {
            var coming = await GetUsernames(lobby.UsersComing);
            var ready = await GetUsernames(lobby.UsersReady);

            var msg = $"**Trainers on the way:**{Environment.NewLine}```{string.Join(Environment.NewLine, coming)}  ```{Environment.NewLine}**Trainers at the raid:**{Environment.NewLine}```{string.Join(Environment.NewLine, ready)}  ```";
            var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
            if (lobbyMessage != null)
            {
                await lobbyMessage.DeleteAsync();
                lobbyMessage = null;
            }

            if (lobbyMessage == null)
            {
                lobbyMessage = await raidLobbyChannel.SendMessageAsync(msg, false, raidMessage);
                lobby.LobbyMessageId = lobbyMessage.Id;
            }
            _config.Save();

            if (lobbyMessage == null)
            {
                Logger.Error($"Failed to set default raid reactions to message {lobby.LobbyMessageId}, couldn't find message...");
                return null;
            }

            lobby.LobbyMessageId = lobbyMessage.Id;
            return lobbyMessage;
        }

        private uint TimeLeft(DateTime etaStart)
        {
            try
            {
                return Convert.ToUInt32((etaStart.AddMinutes(5) - DateTime.Now).Minutes);
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
                return 0;
            }
        }

        private async Task<DiscordMessage> GetRaidMessage(List<SponsoredRaidsConfig> sponsoredRaids, ulong messageId)
        {
            foreach (var sponsored in sponsoredRaids)
            {
                foreach (var channelId in sponsored.ChannelPool)
                {
                    var channel = await _client.GetChannel(channelId);
                    if (channel == null)
                    {
                        Console.WriteLine($"Failed to find channel {channelId}.");
                        continue;
                    }

                    var message = await channel.GetMessage(messageId);
                    if (message == null)
                    {
                        continue;
                    }

                    return message;
                }
            }

            return null;
        }

        private async Task<List<string>> GetUsernames(Dictionary<ulong, RaidLobbyUser> users)
        {
            var list = new List<string>();
            foreach (var item in users)
            {
                var user = await _client.GetUser(item.Key);
                if (user == null)
                {
                    Logger.Error($"Failed to find discord user with id {item.Key}.");
                    continue;
                }

                var timeLeft = TimeLeft(item.Value.EtaStart);
                if (timeLeft == 0)
                {
                    //User is late, send DM.
                    var dm = await _client.SendDirectMessage(user, $"{user.Mention} you're late for the raid, do you want to extend your time? If not please click the red cross button below to remove yourself from the raid lobby.\r\n#{item.Key}#", null);
                    if (dm == null)
                    {
                        Logger.Error($"Failed to send {user.Username} a direct message letting them know they are late for the raid.");
                        continue;
                    }

                    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":five:"));
                    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":keycap_ten:"));
                    await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":x"));
                }

                var eta = (item.Value.Eta != RaidLobbyEta.Here && item.Value.Eta != RaidLobbyEta.NotSet ? $"{timeLeft} minutes" : item.Value.Eta.ToString());
                list.Add($"{user.Username} ({item.Value.Players} account{(item.Value.Players == 1 ? "" : "s")}, ETA: {eta})");
            }
            return list;
        }

        #endregion
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