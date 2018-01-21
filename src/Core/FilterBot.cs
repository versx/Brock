namespace BrockBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using BrockBot.Commands;
    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Net;
    using BrockBot.Services;
    using BrockBot.Utilities;

    using DSharpPlus;

    using Stream = Tweetinvi.Stream;
    using Tweetinvi;
    using Tweetinvi.Models;
    using Tweetinvi.Streaming;
    using Tweetinvi.Streaming.Parameters;

    //TODO: Loop through all arguments for the .feed command. Check if first arg is == remove, if not then assume all arguments are city roles?
    //TODO: Add scanning pokemon list command.
    //TODO: Add rate limiter to prevent spam.
    //TODO: Add .interested command or something similar.
    //TODO: Notify via SMS or Email or Twilio or w/e.
    //TODO: Add support for pokedex # or name for Pokemon and Raid subscriptions.
    //TODO: Subscribe to all Pokemon/Raids/Default.
    //TODO: Keep track of supporters, have a command to check if a paypal email etc or username rather has donated.

    public class FilterBot
    {
        public const string BotName = "Brock";
        public static string DefaultAdvertisementMessage;
        public const int DefaultAdvertisementMessageDifference = 10;
        public const int OneSecond = 1000;
        public const int OneMinute = OneSecond * 60;
        public const int OneHour = OneMinute * 60;
        public const string UnauthorizedAttemptsFileName = "unauthorized_attempts.txt";

        #region Variables

        private DiscordClient _client;
        private readonly Config _config;
        private readonly Database _db;
        private readonly Random _rand;
        private Timer _timer;
        private Timer _adTimer;
        private IFilteredStream _twitterStream;

        private ReminderService _reminderService;

        #endregion

        #region Properties

        public EventLogger Logger { get; set; }

        public CommandList Commands { get; private set; }

        #endregion

        #region Constructor

        public FilterBot()
        {
            Logger = new EventLogger();
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

            _reminderService = new ReminderService(_client, _db);

            //var pokemon = new List<Pokemon>();
            //for (int i = 1; i < 350; i++)
            //{
            //    var poke = _db.Pokemon[i.ToString()];
            //    pokemon.Add(new Pokemon { PokemonId = (uint)i, PokemonName = poke.Name, MinimumIV = 90 });
            //}

            //_db.Servers[0].Subscriptions.Add(new Subscription<Pokemon>(00, pokemon, new List<Pokemon> { new Pokemon { PokemonId = 359, PokemonName = "Absol" } }));
            //_db.Save();
            //Environment.Exit(0);

            //var reminder = new ReminderService();
            //reminder.SetReminder(000, "to do nothing in 25 seconds.");
            //reminder.GetReminders(000);
            //reminder.DeleteReminder(000, 2);
        }

        #endregion

        #region Http Server Events

        private void RaidReceived(object sender, RaidReceivedEventArgs e)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await CheckRaidSubscriptions(e.Raid)) { IsBackground = true }.Start();
#pragma warning restore RECS0165
        }

        private void PokemonReceived(object sender, PokemonReceivedEventArgs e)
        {
#pragma warning disable RECS0165
            new System.Threading.Thread(async x => await CheckPokeSubscriptions(e.Pokemon)) { IsBackground = true }.Start();
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
            AdvertisementTimer_Elapsed(this, null);
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
                     e.Message.Channel.Id == _config.AdminCommandsChannelId ||
                     _db.Lobbies.Exists(x => string.Compare(x.LobbyName, e.Message.Channel.Name, true) == 0))
                     //_db.Servers.Exists(server => server.Lobbies.Exists(x => string.Compare(x.LobbyName, e.Message.Channel.Name, true) == 0)))
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

            await channel.SendMessageAsync($"Zeus was feeling nice today and unbanned {e.Member.Mention}, welcome back! Hopefully you'll learn to behave this time around.");
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
            await Task.CompletedTask;

            //var raidLobbyMessageId = 402530719953453066u;
            //var raidLobbiesChannelId = 392561925793382400u;

            //if (!_config.SponsoredRaids.ChannelPool.Contains(e.Channel.Id) /*||
            //    !_config.RaidLobbiesChannelId = e.Channel.Id)*/) return;

            //if (!e.User.IsBot)
            //{
            //    await e.Message.DeleteReactionAsync(e.Emoji, e.User);
            //}

            ////TODO: Add the user to the list of otw users or remove them etc depending upon the emoji.
            ////TODO: Check if user is already interested in raid, if so send a message etc.
            //switch (e.Emoji.Name)
            //{
            //    case "➡"://"arrow_right":
            //        var msg = $"**Players on the way:** ```OGSkywalker91``` **Players at the raid:** ```{e.User.Username}, versefx```";
            //        var raidMessage = await e.Channel.GetMessage(e.Message.Id);
            //        var embed = raidMessage.Embeds[0];

            //        var raidLobbiesChannel = await _client.GetChannel(raidLobbiesChannelId);
            //        if (raidLobbiesChannel == null)
            //        {
            //            Logger.Error($"Failed to retrieve the raid lobbies channel with id {raidLobbiesChannelId}.");
            //            return;
            //        }

            //        var raidLobbyMessage = await raidLobbiesChannel.GetMessage(raidLobbyMessageId);
            //        if (raidLobbyMessage != null)
            //        {
            //            await raidLobbyMessage.DeleteAsync();
            //            raidLobbyMessage = null;
            //        }

            //        if (raidLobbyMessage == null)
            //        {
            //            raidLobbyMessage = await raidLobbiesChannel.SendMessageAsync(msg, false, embed);
            //        }

            //        await _client.SetDefaultRaidReactions(raidLobbyMessage);
            //        break;
            //    case "white_check_mark":
            //        break;
            //    case "x":
            //        var raidLobbiesChannel = await _client.GetChannel(raidLobbiesChannelId);
            //        if (raidLobbiesChannel == null)
            //        {
            //            Logger.Error($"Failed to retrieve the raid lobbies channel with id {raidLobbiesChannelId}.");
            //            return;
            //        }

            //        var raidLobbyMessage = await raidLobbiesChannel.GetMessage(raidLobbyMessageId);
            //        if (raidLobbyMessage != null)
            //        {
            //            await raidLobbyMessage.DeleteAsync();
            //            raidLobbyMessage = null;
            //        }

            //        if (raidLobbyMessage == null)
            //        {
            //            raidLobbyMessage = await raidLobbiesChannel.SendMessageAsync(msg, false, embed);
            //        }
            //        break;
            //}
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
                        //foreach (var server in _db.Servers)
                        //{
                            foreach (var lobby in _db.Lobbies)
                            {
                                if (lobby.IsExpired)
                                {
                                    var channel = await _client.GetChannel(lobby.ChannelId);
                                    if (channel == null)
                                    {
                                        Logger.Error($"Failed to delete expired raid lobby channel because channel {lobby.LobbyName} ({lobby.ChannelId}) does not exist.");
                                        continue;
                                    }
                                    //await channel.DeleteAsync($"Raid lobby {lobby.LobbyName} ({lobby.ChannelId}) no longer needed.");
                                }
                                await _client.UpdateLobbyStatus(lobby);
                            }
                        //}

                        //_db.Servers.ForEach(server => server.Lobbies.RemoveAll(lobby => lobby.IsExpired));
                        _db.Lobbies.RemoveAll(lobby => lobby.IsExpired);
                    }
#pragma warning disable RECS0022
                    catch { }
#pragma warning restore RECS0022
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
            _twitterStream.StreamStarted += (sender, e) => Console.WriteLine("Successfully started.");
            _twitterStream.StreamStopped += (sender, e) => Console.WriteLine($"Stream stopped.\r\n{e.Exception}\r\n{e.DisconnectMessage}");
            _twitterStream.DisconnectMessageReceived += (sender, e) => Console.WriteLine($"Disconnected.\r\n{e.DisconnectMessage}");
            _twitterStream.WarningFallingBehindDetected += (sender, e) => Console.WriteLine($"Warning Falling Behind Detected: {e.WarningMessage}");
            //stream.AddFollow(2839430431);
            //stream.AddFollow(358652328);
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

            await _client.DisconnectAsync();
            _client.Dispose();
            _client = null;
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
                        args.Add(_reminderService);
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
                    Logger.Info($"Command(s) {string.Join(", ", cmds)} was successfully registered.");

                    return true;
                }

                Logger.Error($"Failed to register command(s) {string.Join(", ", cmds)}");
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

                switch (Commands[command.Name].PermissionLevel)
                {
                    case CommandPermissionLevel.Admin:
                        var isOwner = message.Author.Id == _config.OwnerId;
                        if (!isOwner)
                        {
                            LogUnauthorizedAccess(message.Author);
                            await message.RespondAsync($"{message.Author.Mention} is not authorized to execute these type of commands, your unique user id has been logged.");
                            return;
                        }
                        break;
                    case CommandPermissionLevel.Supporter:
                        var isSupporter = await _client.HasSupporterRole(message.Author.Id, _config.SupporterRoleId);
                        if (!isSupporter)
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
            eb.WithTitle("Help Command Information (Type ");

            var categories = GetCommandsByCategory();
            if (command.HasArgs && command.Args.Count == 1)
            {
                var category = ParseCategory(command.Args[0]);
                if (string.IsNullOrEmpty(category))
                {
                    await message.RespondAsync("You have entered an invalid help command category.");
                    return;
                }

                eb.AddField(category, "|");
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

        private async Task CheckSponsoredRaids(DiscordMessage message)
        {
            //TODO: Add whittier raids and legendary raids channel ids.
            //TODO: Add whittier sponsored raids channel webhook.
            if (!_config.SponsoredRaids.ChannelPool.Contains(message.ChannelId)) return;

            //await _client.SetDefaultRaidReactions(message);

            foreach (var embed in message.Embeds)
            {
                //foreach (var sponsored in _config.SponsoredRaids)
                //{
                //    if (!sponsored.ChannelPool.Contains(message.ChannelId)) continue;

                //    foreach (var keyword in sponsored.Keywords)
                //    {
                //        if (embed.Description.Contains(keyword))
                //        {
                //            await _client.SendMessage(sponsored.WebHook, string.Empty, embed);
                //        }
                //    }
                //}

                foreach (var keyword in _config.SponsoredRaids.Keywords)
                {
                    if (embed.Description.Contains(keyword))
                    {
                        await _client.SendMessage(_config.SponsoredRaids.WebHook, string.Empty, embed);
                        break;
                    }
                }
            }
        }

        #region Subscriptions

        private async Task CheckPokeSubscriptions(PokemonData pkmn)
        {
            if (_db == null)
            {
                Logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            foreach (var user in _db.Subscriptions)
            {
                if (!user.Enabled) continue;

                discordUser = await _client.GetUser(user.UserId);
                if (discordUser == null) continue;

                if (!user.Pokemon.Exists(x => x.PokemonId == pkmn.PokemonId)) continue;
                var subscribedPokemon = user.Pokemon.Find(x => x.PokemonId == pkmn.PokemonId);

                if (!_db.Pokemon.ContainsKey(subscribedPokemon.PokemonId.ToString())) continue;
                var pokemon = _db.Pokemon[subscribedPokemon.PokemonId.ToString()];
                if (pokemon == null) continue;

                //if (ignoreMissingCPIV && (pkmn.CP == "?" || pkmn.IV == "?") && subscribedPokemon.MinimumIV > 0) continue;

                var matchesIV = false;
                if (pkmn.IV != "?")
                {
                    if (!int.TryParse(pkmn.IV.Replace("%", ""), out int resultIV))
                    {
                        Logger.Error($"Failed to parse pokemon IV value '{pkmn.IV}', skipping filter check.");
                        continue;
                    }

                    matchesIV |= resultIV >= subscribedPokemon.MinimumIV;
                }

                //var matchesCP = false;
                //if (pkmn.CP != "?")
                //{
                //    if (!int.TryParse(pkmn.CP, out int resultCP))
                //    {
                //        Logger.Error($"Failed to parse pokemon CP {pkmn.CP}, skipping filter check.");
                //        continue;
                //    }

                //    matchesCP |= resultCP >= subscribedPokemon.MinimumCP;
                //}

                if (pkmn.IV == "?" && subscribedPokemon.MinimumIV == 0)
                {
                    matchesIV = true;
                }

                if (!matchesIV) continue;
                //if (!matchesIV || !matchesCP) continue;

                Logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} CP{pkmn.CP} {pkmn.IV}% IV has spawned...");

                var embed = await BuildEmbedPokemon(pkmn, user.UserId);
                if (embed == null) continue;

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(10);
            }
        }

        private async Task CheckRaidSubscriptions(RaidData raid)
        {
            if (_db == null)
            {
                Logger.Error($"Database is not initialized...");
                return;
            }

            DiscordUser discordUser;
            foreach (var user in _db.Subscriptions)
            {
                if (!user.Enabled) continue;

                discordUser = await _client.GetUser(user.UserId);
                if (discordUser == null) continue;

                if (!user.Raids.Exists(x => x.PokemonId == raid.PokemonId)) continue;
                var subscribedRaid = user.Raids.Find(x => x.PokemonId == raid.PokemonId);

                if (!_db.Pokemon.ContainsKey(subscribedRaid.PokemonId.ToString())) continue;
                var pokemon = _db.Pokemon[subscribedRaid.PokemonId.ToString()];
                if (pokemon == null) continue;

                if (subscribedRaid.PokemonId != raid.PokemonId) continue;

                Logger.Info($"Notifying user {discordUser.Username} that a {pokemon.Name} raid is available...");

                var embed = await BuildEmbedRaid(raid, user.UserId);
                if (embed == null) continue;

                Notify(pokemon.Name, embed);

                await _client.SendDirectMessage(discordUser, string.Empty, embed);
                await Utils.Wait(10);
            }
        }

        #endregion

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

        private async Task SendStartupMessage()
        {
            var randomWelcomeMessage = _config.StartupMessages[_rand.Next(0, _config.StartupMessages.Count - 1)];
            await _client.SendMessage(_config.StartupMessageWebHook, randomWelcomeMessage);
        }

        private async Task DisplaySettings()
        {
            Logger.Trace($"FilterBot::DisplaySettings");

            Console.WriteLine($"********** Current Settings **********");
            var owner = await _client.GetUserAsync(_config.OwnerId);
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
                    var user = await _client.GetUserAsync(sub.UserId);
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
                Console.WriteLine("Raid Lobbies:");
                Console.WriteLine();
                foreach (var lobby in _db.Lobbies)
                {
                    Console.WriteLine($"Lobby Name: {lobby.LobbyName}");
                    Console.WriteLine($"Raid Boss: {lobby.PokemonName}");
                    Console.WriteLine($"Gym Name: {lobby.GymName}");
                    Console.WriteLine($"Address: {lobby.Address}");
                    Console.WriteLine($"Start Time: {lobby.StartTime}");
                    Console.WriteLine($"Expire Time: {lobby.ExpireTime}");
                    Console.WriteLine($"Minutes Left: {lobby.MinutesLeft}");
                    Console.WriteLine($"Is Expired: {lobby.IsExpired}");
                    Console.WriteLine($"# Users Checked-In: {lobby.NumUsersCheckedIn}");
                    Console.WriteLine($"# Users On The Way: {lobby.NumUsersOnTheWay}");
                    Console.WriteLine($"Original Raid Message Id: {lobby.OriginalRaidMessageId}");
                    Console.WriteLine($"Pinned Raid Message Id{lobby.PinnedRaidMessageId}");
                    Console.WriteLine($"Channel Id: {lobby.ChannelId}");
                    Console.WriteLine($"Raid Lobby User List:");
                    foreach (var lobbyUser in lobby.UserCheckInList)
                    {
                        Console.WriteLine($"User Id: {lobbyUser.UserId}");
                        Console.WriteLine($"Is OnTheWay: {lobbyUser.IsOnTheWay}");
                        Console.WriteLine($"OnTheWay Time: {lobbyUser.OnTheWayTime}");
                        Console.WriteLine($"Is Checked-In: {lobbyUser.IsCheckedIn}");
                        Console.WriteLine($"Check-In Time: {lobbyUser.CheckInTime}");
                        Console.WriteLine($"User Count: {lobbyUser.UserCount}");
                        Console.WriteLine($"ETA: {lobbyUser.ETA}");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
                Console.WriteLine();
            //}
            Console.WriteLine($"**************************************");
        }

        private void Notify(string pokemon, DiscordEmbed embed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("***********************************");
            Console.WriteLine($"********** {pokemon} FOUND **********");
            Console.WriteLine("***********************************");
            Console.WriteLine(DateTime.Now.ToString());
            //Console.WriteLine("Title: \t\t{0}", embed.Title); //DIRECTIONS
            Console.WriteLine(embed.Description); //CP, IV, etc...
            Console.WriteLine(embed.Url); //GMaps link
            Console.WriteLine("***********************************");
            Console.WriteLine();
            Console.ResetColor();
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

        private void LogUnauthorizedAccess(DiscordUser user)
        {
            try
            {
                File.AppendAllText(UnauthorizedAttemptsFileName, $"{user.Username}:{user.Id}\r\n");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task SendTwitterNotification(long ownerId, string url)
        {
            if (!_config.TwitterUpdates.PostTwitterUpdates) return;

            Console.WriteLine($"Tweet [Owner={ownerId}, Url={url}]");
            await _client.SendMessage(_config.TwitterUpdates.UpdatesChannelWebHook, url);
        }

        private async void AdvertisementTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await CheckFeedStatus();

            //await CheckSupporterStatus(_client.Guilds[0].Id);

            await PostAdvertisement();
        }

        private async Task<DiscordEmbed> BuildEmbedRaid(RaidData raid, ulong userId)
        {
            var pkmn = _db.Pokemon[raid.PokemonId.ToString()];
            if (pkmn == null)
            {
                Logger.Error($"Failed to lookup Raid Pokemon '{raid.PokemonId}' in database.");
                return null;
            }

            var loc = Utils.GetGoogleAddress(raid.Latitude, raid.Longitude, _config.GmapsKey);
            var user = await _client.GetMemberFromUserId(userId);
            if (user == null)
            {
                Logger.Error($"Failed to get discord member object from user id {userId}.");
                return null;
            }

            if (loc != null)
            {
                if (!_client.HasRole(user, loc.City.Replace("Rancho Cucamonga", "Upland")))
                {
                    Logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role.");
                    return null;
                }
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description = $"{pkmn.Name} raid is available!",
                Url = string.Format(HttpServer.GoogleMaps, raid.Latitude, raid.Longitude),
                ImageUrl = string.Format(HttpServer.GoogleMapsImage, raid.Latitude, raid.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, raid.PokemonId),
                Color = DiscordColor.Red
            };

            var fixedEndTime = DateTime.Parse(raid.EndTime.ToLongTimeString());
            var remaining = GetRaidTimeRemaining(fixedEndTime).Minutes;
            eb.AddField($"{pkmn.Name} (#{raid.PokemonId})", $"Level {raid.Level} ({Convert.ToInt32(raid.CP ?? "0").ToString("N0")} CP)");
            eb.AddField("Started:", raid.StartTime.ToLongTimeString());
            eb.AddField("Available Until:", $"{raid.EndTime.ToLongTimeString()} ({remaining} minutes left)");

            var fastMove = _db.Movesets.ContainsKey(raid.FastMove) ? _db.Movesets[raid.FastMove] : null;
            if (fastMove != null)
            {
                eb.AddField("Fast Move:", $"{fastMove.Name} ({fastMove.Type}, {fastMove.Damage} dmg, {fastMove.DamagePerSecond} dps)");
            }

            var chargeMove = _db.Movesets.ContainsKey(raid.ChargeMove) ? _db.Movesets[raid.ChargeMove] : null;
            if (chargeMove != null)
            {
                eb.AddField("Charge Move:", $"{chargeMove.Name} ({chargeMove.Type}, {chargeMove.Damage} dmg, {chargeMove.DamagePerSecond} dps)");
            }

            if (loc != null)
            {
                eb.AddField("Address:", loc.Address);
            }
            eb.AddField("Location:", $"{raid.Latitude},{raid.Longitude}");
            eb.WithImageUrl(string.Format(HttpServer.GoogleMapsImage, raid.Latitude, raid.Longitude));
            var embed = eb.Build();

            return embed;
        }

        private async Task<DiscordEmbed> BuildEmbedPokemon(PokemonData pokemon, ulong userId)
        {
            var pkmn = _db.Pokemon[pokemon.PokemonId.ToString()];
            if (pkmn == null)
            {
                Logger.Error($"Failed to lookup Pokemon '{pokemon.PokemonId}' in database.");
                return null;
            }

            var loc = Utils.GetGoogleAddress(pokemon.Latitude, pokemon.Longitude, _config.GmapsKey);
            var user = await _client.GetMemberFromUserId(userId);
            if (user == null)
            {
                Logger.Error($"Failed to get discord member object from user id {userId}.");
                return null;
            }

            if (loc != null)
            {
                if (!_client.HasRole(user, loc.City.Replace("Rancho Cucamonga", "Upland")))
                {
                    Logger.Debug($"Skipping notification for user {user.DisplayName} ({user.Id}) for Pokemon {pkmn.Name} because they do not have the city role.");
                    return null;
                }
            }

            var eb = new DiscordEmbedBuilder
            {
                Title = loc == null || string.IsNullOrEmpty(loc.City) ? "DIRECTIONS" : loc.City,
                Description = $"{pkmn.Name}{Helpers.GetPokemonGender(pokemon.Gender)} {pokemon.CP}CP {pokemon.IV} LV{pokemon.PlayerLevel} has spawned!",
                Url = string.Format(HttpServer.GoogleMaps, pokemon.Latitude, pokemon.Longitude),
                ImageUrl = string.Format(HttpServer.GoogleMapsImage, pokemon.Latitude, pokemon.Longitude),
                ThumbnailUrl = string.Format(HttpServer.PokemonImage, pokemon.PokemonId),
                Color = BuildColor(pokemon.IV)
            };

            eb.AddField($"{pkmn.Name} (#{pokemon.PokemonId}, {pokemon.Gender})", $"CP: {pokemon.CP} IV: {pokemon.IV} (Sta: {pokemon.Stamina}/Atk: {pokemon.Attack}/Def: {pokemon.Defense}) LV: {pokemon.PlayerLevel}");
            eb.AddField("Available Until:", $"{pokemon.DespawnTime.ToLongTimeString()} ({pokemon.SecondsLeft} remaining)");
            if (loc != null)
            {
                eb.AddField("Address:", loc.Address);
            }
            eb.AddField("Location:", $"{pokemon.Latitude},{pokemon.Longitude}");
            eb.WithImageUrl(string.Format(HttpServer.GoogleMapsImage, pokemon.Latitude, pokemon.Longitude) + $"&key={_config.GmapsKey}");
            var embed = eb.Build();

            return embed;
        }

        private DiscordColor BuildColor(string iv)
        {
            if (int.TryParse(iv.Substring(0, iv.Length - 1), out int result))
            {
                if (result == 100)
                    return DiscordColor.Green;
                else if (result >= 90 && result < 100)
                    return DiscordColor.Orange;
                else if (result < 90)
                    return DiscordColor.Yellow;
            }

            return DiscordColor.White;
        }

        #endregion

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

                var owner = await _client.GetUserAsync(_config.OwnerId);
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

        private TimeSpan GetRaidTimeRemaining(DateTime endTime)
        {
            var start = DateTime.Now;
            var end = DateTime.Parse(endTime.ToLongTimeString());
            var remaining = TimeSpan.FromTicks(end.Ticks - start.Ticks);
            return remaining;
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