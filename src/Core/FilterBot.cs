namespace BrockBot
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    //using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using BrockBot.Commands;
    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    using DSharpPlus;

    //TODO: Add command and create new instance even if no parameters given for the constructor.
    //TODO: .uptime command - Been up for: 6 days, 7 hours, 42 minutes, and 52 seconds (since 2017-11-10 21:12:13 UTC)
    //TODO: .invite Generate a link that you can use to add BrockBot to your own server.
    //TODO: Redo bot messages as embed messages.
    //TODO: Notify via SMS or Twilio or w/e.
    //TODO: Add .interested command or something similar.

    public class FilterBot
    {
        #region Variables

        private DiscordClient _client;
        private readonly Database _db;
        private readonly Config _config;
        private readonly Random _rand;
        private Timer _timer;

        #endregion

        #region Properties

        public EventLogger Logger { get; set; }

        public CommandList Commands { get; private set; }

        #endregion

        #region Constructor

        public FilterBot()
        {
            Logger = new EventLogger();
            //Logger.Trace($"FilterBot::FilterBot");
            //Logger.Info($"Logging started for {AssemblyUtils.AssemblyName} v{AssemblyUtils.AssemblyVersion} by {AssemblyUtils.CompanyName}...");
            Commands = new CommandList();

            _db = Database.Load();
            _config = Config.Load();
            _rand = new Random();
        }

        #endregion

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_Ready [{e.Client.CurrentUser.Username}]");

            foreach (var guild in e.Client.Guilds)
            {
                if (!_db.ContainsKey(guild.Key))
                {
                    _db.Servers.Add(new Server(guild.Key, new List<RaidLobby>(), new List<Subscription>()));
                }
            }

            await DisplaySettings();

            if (_config.SendStartupMessage)
            {
                var randomWelcomeMessage = _config.StartupMessages[_rand.Next(0, _config.StartupMessages.Count - 1)];
                await SendMessage(_config.StartupMessageWebHook, randomWelcomeMessage);
            }

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_MessageCreated [Username={e.Message.Author.Username} Message={e.Message.Content}]");

            //Console.WriteLine($"Message recieved from server {e.Guild.Name} #{e.Message.Channel.Name}: {e.Message.Author.Username} (IsBot: {e.Message.Author.IsBot}) {e.Message.Content}");

            if (e.Message.Author.Id == _client.CurrentUser.Id) return;

            //if (e.Message.Channel == null) return;
            //var server = _db.Servers[e.Message.Channel.GuildId];
            //if (server == null) return;

            if (e.Message.Author.IsBot)
            {
                await CheckSponsorRaids(e.Message);
                await CheckSubscriptions(e.Message);
            }
            else if (e.Message.Channel.Name == _config.CommandsChannel)
            {
                await ParseCommand(e.Message);
            }
            else if (_db.Servers.Exists(server => server.Lobbies.Exists(x => string.Compare(x.LobbyName, e.Message.Channel.Name, true) == 0)))
            {
                await ParseCommand(e.Message);
                //TODO: Implement RaidLobby specific commands, .list, etc..
            }
        }

        private async Task Client_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_DmChannelCreated [{e.Channel.Name}]");

            var msg = await e.Channel.GetMessageAsync(e.Channel.LastMessageId);
            if (msg == null)
            {
                Utils.LogError(new Exception($"Failed to find last direct message from id {e.Channel.LastMessageId}."));
                return;
            }

            await ParseCommand(msg);
        }

        private async Task Client_GuildBanAdded(GuildBanAddEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildBanAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = _client.GetChannelByName(_config.CommandsChannel);
            if (channel == null)
            {
                Utils.LogError(new Exception($"Failed to find channel {_config.CommandsChannel}."));
                return;
            }

            await channel.SendMessageAsync($"OH SNAP! The ban hammer was just dropped on {e.Member.Mention}, cya!");
        }

        private async Task Client_GuildBanRemoved(GuildBanRemoveEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildBanRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            var channel = _client.GetChannelByName(_config.CommandsChannel);
            if (channel == null)
            {
                Utils.LogError(new Exception($"Failed to find channel {_config.CommandsChannel}."));
                return;
            }

            await channel.SendMessageAsync($"Zeus was feeling nice today and unbanned {e.Member.Mention}, welcome back! Hopefully you'll learn to behave this time around.");
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildMemberAdded [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyNewMemberJoined)
            {
                var channel = _client.GetChannelByName(_config.CommandsChannel);
                if (channel == null)
                {
                    Utils.LogError(new Exception($"Failed to find channel {_config.CommandsChannel}."));
                    return;
                }

                await channel.SendMessageAsync($"Everyone let's welcome {e.Member.Mention} to the server! We've been waiting for you!");
            }

            if (_config.SendWelcomeMessage)
            {
                await SendBotIntroMessage(e.Member);
            }
        }

        private async Task Client_GuildMemberRemoved(GuildMemberRemoveEventArgs e)
        {
            Logger.Trace($"FilterBot::Client_GuildMemberRemoved [Guild={e.Guild.Name}, Username={e.Member.Username}]");

            if (_config.NotifyMemberLeft)
            {
                var channel = _client.GetChannelByName(_config.CommandsChannel);
                if (channel == null)
                {
                    Utils.LogError(new Exception($"Failed to find channel {_config.CommandsChannel}."));
                    return;
                }
                await channel.SendMessageAsync($"Sorry to see you go {e.Member.Mention}, hope to see you back soon!");
            }
        }

        #endregion

        #region Public Methods

        public async Task Start()
        {
            Logger.Trace($"FilterBot::Start");

            if (_client != null)
            {
                Logger.Warn($"{AssemblyUtils.AssemblyName} is already started, no need to start again.");
                return;
            }

            _client = new DiscordClient(new DiscordConfiguration
            {
                AutoReconnect = true,
                //DiscordBranch = Branch.Stable,
                LogLevel = LogLevel.Debug,
                Token = _config.AuthToken,
                TokenType = TokenType.Bot
            });

            _client.MessageCreated += Client_MessageCreated;
            _client.Ready += Client_Ready;
            _client.DmChannelCreated += Client_DmChannelCreated;
            _client.GuildMemberAdded += Client_GuildMemberAdded;
            _client.GuildMemberRemoved += Client_GuildMemberRemoved;
            _client.GuildBanAdded += Client_GuildBanAdded;
            _client.GuildBanRemoved += Client_GuildBanRemoved;

            if (_timer == null)
            {
                _timer = new Timer(15000);
#pragma warning disable RECS0165
                _timer.Elapsed += async (sender, e) =>
#pragma warning restore RECS0165
                {
                    if (_client == null) return;
                    try
                    {
                        foreach (var server in _db.Servers)
                        {
                            foreach (var lobby in server.Lobbies)
                            {
                                if (lobby.IsExpired)
                                {
                                    var channel = await _client.GetChannel(lobby.ChannelId);
                                    if (channel == null)
                                    {
                                        Utils.LogError(new Exception($"Failed to delete expired raid lobby channel because channel {lobby.LobbyName} ({lobby.ChannelId}) does not exist."));
                                        continue;
                                    }
                                    //await channel.DeleteAsync($"Raid lobby {lobby.LobbyName} ({lobby.ChannelId}) no longer needed.");
                                }
                                await _client.UpdateLobbyStatus(lobby);
                            }
                        }

                        _db.Servers.ForEach(server => server.Lobbies.RemoveAll(lobby => lobby.IsExpired));
                    }
#pragma warning disable RECS0022
                    catch { }
#pragma warning restore RECS0022
                };
                _timer.Start();
            }

            Logger.Info("Connecting to discord server...");
            await _client.ConnectAsync();
            await Task.Delay(-1);
        }

        public async Task Stop()
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
                CommandAttribute attr = new CommandAttribute();
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
                    return true;
                }

                Logger.Error($"Failed to register command(s) {string.Join(", ", cmds)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public void UnregisterCommand(string[] cmdNames)
        {
            if (!Commands.ContainsKey(cmdNames))
            {
                Commands.Remove(cmdNames);
                return;
            }

            Logger.Error($"Failed to unregister command {string.Join(", ", cmdNames)}");
        }

        #endregion

        #region Private Methods

        public async Task ParseCommand(DiscordMessage message)
        {
            Logger.Trace($"FilterBot::ParseCommand [Message={message.Content}]");

            var command = new Command(_config.CommandsPrefix, message.Content);
            if (!command.ValidCommand && !message.Author.IsBot) return;

            if (Commands.ContainsKey(command.Name))
            {
                var isOwner = message.Author.Id == _config.OwnerId;
                if ((Commands[command.Name].AdminCommand && isOwner) || !Commands[command.Name].AdminCommand)
                {
                    await Commands[command.Name].Execute(message, command);
                }
                //else
                //{
                //    //TODO: You are not the owner so your commands are not recognized.
                //}
            }

            _db.Save();
        }

        private async Task CheckSponsorRaids(DiscordMessage message)
        {
            if (!_config.SponsorRaidChannelPool.Contains(message.Channel.Id)) return;

            foreach (DiscordEmbed embed in message.Embeds)
            {
                foreach (var keyword in _config.SponsorRaidKeywords)
                {
                    if (embed.Description.Contains(keyword))
                    {
                        await SendMessage(_config.SponsorRaidsWebHook, string.Empty, embed);
                        break;
                    }
                }
            }
        }

        private async Task CheckSubscriptions(DiscordMessage message)
        {
            if (message.Channel == null) return;
            var server = _db[message.Channel.GuildId];
            if (server == null) return;

            DiscordUser discordUser;
            foreach (var user in server.Subscriptions)
            {
                if (!user.Enabled) continue;

                discordUser = await _client.GetUserAsync(user.UserId);
                if (discordUser == null) continue;

                if (!user.ChannelIds.Contains(message.Channel.Id)) continue;

                foreach (var poke in user.Pokemon)
                {
                    if (!_db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;
                    var pokemon = _db.Pokemon[poke.PokemonId.ToString()];

                    if (!message.Author.Username.ToLower().Contains(pokemon.Name.ToLower())) continue;

                    Console.WriteLine($"Notifying user {discordUser.Username} that a {pokemon.Name} has appeared in channel #{message.Channel.Name}...");

                    var msg = $"A wild {pokemon.Name} has appeared in channel {message.Channel.Mention}!\r\n\r\n" + message.Content;
                    Notify(discordUser, msg, poke, message.Embeds[0]);

                    await SendDirectMessage(discordUser, msg, message.Embeds.Count == 0 ? null : message.Embeds[0]);
                }
            }
        }

        private async Task SendDirectMessage(DiscordUser user, string message, DiscordEmbed embed)
        {
            var dm = await _client.CreateDmAsync(user);
            if (dm != null)
            {
                await dm.SendMessageAsync(message, false, embed);
            }
        }

        private async Task SendMessage(string webHookUrl, string message, DiscordEmbed embed = null)
        {
            var data = Utils.GetWebHookData(webHookUrl);
            if (data == null) return;

            var guildId = Convert.ToUInt64(Convert.ToString(data["guild_id"]));
            var channelId = Convert.ToUInt64(Convert.ToString(data["channel_id"]));

            var guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Guild does not exist!");
                Console.ResetColor();
                return;
            }
            //var channel = guild.GetChannel(channelId);
            var channel = await _client.GetChannelAsync(channelId);
            if (channel == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Channel does not exist!");
                Console.ResetColor();
                return;
            }

            await channel.SendMessageAsync(message, false, embed);
        }

        private async Task SendBotIntroMessage(DiscordUser user)
        {
            Logger.Trace($"FilterBot::SendBotIntroMessage [Username={user.Username}]");

            await SendDirectMessage
            (
                user,
                _config.WelcomeMessage
                    .Replace("{username}", user.Username),
                //$"Hello {user.Username}, and welcome to versx's discord server!\r\n" +
                //"I am here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies.\r\n\r\n" +
                //"To see a full list of my available commands please send me a direct message containing `.help`.",
                null
            );
        }

        private async Task DisplaySettings()
        {
            Logger.Trace($"FilterBot::DisplaySettings");

            Console.WriteLine($"********** Current Settings **********");
            var owner = await _client.GetUserAsync(_config.OwnerId);
            Console.WriteLine($"Owner: {owner?.Username} ({_config.OwnerId})");
            Console.WriteLine($"Authentication Token: {_config.AuthToken}");
            Console.WriteLine($"Commands Channel: {_config.CommandsChannel}");
            Console.WriteLine($"Commands Prefix: {_config.CommandsPrefix}");
            Console.WriteLine($"Allow Team Assignment: {(_config.AllowTeamAssignment ? "Yes" : "No")}");
            Console.WriteLine($"Available Team Roles: {(string.Join(", ", _config.AvailableTeamRoles))}");
            Console.WriteLine($"Notify New Member Joined: {(_config.NotifyNewMemberJoined ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Left: {(_config.NotifyMemberLeft ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Banned: {(_config.NotifyMemberBanned ? "Yes" : "No")}");
            Console.WriteLine($"Notify Member Unbanned: {(_config.NotifyMemberUnbanned ? "Yes" : "No")}");
            Console.WriteLine($"Send Startup Message: {(_config.SendStartupMessage ? "Yes" : "No")}");
            Console.WriteLine($"Startup Messages: {string.Join(", ", _config.StartupMessages)}");
            Console.WriteLine($"Startup Message WebHook: {_config.StartupMessageWebHook}");
            Console.WriteLine($"Send Welcome Message: {_config.SendWelcomeMessage}");
            Console.WriteLine($"Welcome Message: {_config.WelcomeMessage}");
            Console.WriteLine($"Sponsor Raid Channel Pool: {string.Join(", ", _config.SponsorRaidChannelPool)}");
            Console.WriteLine($"Sponsor Raid Keywords: {string.Join(", ", _config.SponsorRaidKeywords)}");
            Console.WriteLine($"Sponsor Raids WebHook: {_config.SponsorRaidsWebHook}");
            Console.WriteLine();
            foreach (var server in _db.Servers)
            {
                Console.WriteLine($"Guild Id: {server.GuildId}");
                Console.WriteLine("Subscriptions:");
                Console.WriteLine();
                foreach (var sub in server.Subscriptions)
                {
                    var user = await _client.GetUserAsync(sub.UserId);
                    if (user != null)
                    {
                        Console.WriteLine($"Enabled: {(sub.Enabled ? "Yes" : "No")}");
                        Console.WriteLine($"Username: {user.Username}");
                        Console.WriteLine($"Pokemon Notifications:");
                        foreach (var poke in sub.Pokemon)
                        {
                            if (!_db.Pokemon.ContainsKey(poke.PokemonId.ToString())) continue;
                            Console.WriteLine(_db.Pokemon[poke.PokemonId.ToString()].Name + $" ({poke})");
                        }
                        Console.WriteLine($"Channel Subscriptions: {string.Join(", ", sub.ChannelIds)}");
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                }
                Console.WriteLine();
                Console.WriteLine("Raid Lobbies:");
                Console.WriteLine();
                foreach (var lobby in server.Lobbies)
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
            }
            Console.WriteLine($"**************************************");
        }

        private void Notify(DiscordUser user, string message, Pokemon pokemon, DiscordEmbed embed)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("***********************************");
            Console.WriteLine($"********** {pokemon.PokemonName} FOUND **********");
            Console.WriteLine("***********************************");
            Console.WriteLine(DateTime.Now.ToString());
            //Console.WriteLine("Title: \t\t{0}", embed.Title); //DIRECTIONS
            Console.WriteLine(embed.Description); //CP, IV, etc...
            Console.WriteLine(embed.Url); //GMaps link
            Console.WriteLine("***********************************");
            Console.WriteLine();
            Console.ResetColor();

            Console.WriteLine($"Alerting discord user {user.Username} of {message}");
        }

        #endregion
    }
}