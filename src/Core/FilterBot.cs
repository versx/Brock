namespace BrockBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using BrockBot.Commands;
    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    //TODO: Notify via SMS or Twilio or w/e.
    //TODO: Pokemon info lookup.
    //TODO: Testing
    //TODO: Possibly change .checkin to .here or .ready?
    //TODO: Added a .interested command or something similar.
    //TODO: Raid channel specific commands, !list coming etc.

    public class FilterBot
    {
        #region Variables

        private DiscordClient _client;
        private readonly Database _db;
        private readonly Config _config;
        private readonly Random _rand;
        private Timer _timer;

        private readonly string[] _wakeupMessages =
        {
            "Whoa, whoa...alright I'm awake.",
            "No need to push, I'm going...",
            "That was a weird dream, wait a minute...",
            //"Circuit overload, malfunktshun."
            "Circuits fully charged, let's do this!",
            "What is this place? How did I get here?",
            "Looks like we're not in Kansas anymore...",
            "Hey...watch where you put those mittens!"
        };

        #endregion

        #region Properties

        public Dictionary<string, ICustomCommand> Commands { get; private set; }

        #endregion

        #region Constructor

        public FilterBot()
        {
            _db = Database.Load();
            _config = Config.Load();
            _rand = new Random();
        }

        #endregion

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            await DisplaySettings();

            if (_config.SendStartupMessage)
            {
                var randomWelcomeMessage = _wakeupMessages[_rand.Next(0, _wakeupMessages.Length - 1)];
                await SendMessage(_config.StartupMessageWebHook, randomWelcomeMessage);
            }

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
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
            if (_client != null)
            {
                Console.WriteLine($"{AssemblyUtils.AssemblyName} already started, no need to start again.");
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

            if (Commands == null)
            {
                Commands = new Dictionary<string, ICustomCommand>()
                {
                    { "demo", new DemoCommand() },
                    { "help", new HelpCommand() },
                    { "info", new InfoCommand(_client, _db) },
                    { "version", new VersionCommand() },
                    { "setup", new AddCommand(_client, _db) },
                    { "add", new AddCommand(_client, _db) },
                    { "remove", new RemoveCommand(_client, _db) },
                    { "sub", new SubscribeCommand(_db) },
                    { "unsub", new UnsubscribeCommand(_db) },
                    { "enable", new EnableDisableCommand(_db, true) },
                    { "disable", new EnableDisableCommand(_db, false) },
                    { "team", new TeamCommand(_client, _config) },
                    { "create_roles", new CreateRolesCommand(_client) },
                    { "delete_roles", new DeleteRolesCommand(_client) },
                    { "lobby", new CreateRaidLobbyCommand(_client, _db) },
                    { "checkin", new RaidLobbyCheckInCommand(_client, _db) },
                    { "ontheway", new RaidLobbyOnTheWayCommand(_client, _db) },
                    { "cancel", new RaidLobbyCancelCommand(_client, _db) },
                    { "list", new RaidLobbyListUsersCommand(_client, _db) },
                    { "restart", new RestartCommand() },
                    { "shutdown", new ShutdownCommand() }
                };
            }

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

            Console.WriteLine("Connecting to discord server...");
            await _client.ConnectAsync();
            await Task.Delay(-1);
        }

        public async Task Stop()
        {
            if (_client == null)
            {
                Console.WriteLine($"{AssemblyUtils.AssemblyName} has not been started, therefore it cannot be stopped.");
                return;
            }

            Console.WriteLine($"Shutting down {AssemblyUtils.AssemblyName}...");

            await _client.DisconnectAsync();
            _client.Dispose();
            _client = null;
        }

        #endregion

        #region Private Methods

        public async Task ParseCommand(DiscordMessage message)
        {
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
            if (_config.SponsorRaidChannelPool.Contains(message.Channel.Id))
            {
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
            //switch (message.Channel.Id)
            //{
            //    case 375047782827950092: //Legendary_Raids
            //    case 366049816188420096: //Upland_Raids
            //    case 374809552928899082: //Upland_Legendary_Raids
            //    case 366359725857832973: //Ontario_Raids
            //    case 374817863690747905: //Ontario_Legendary_Raids
            //    case 366049617642520596: //Pomona_Raids
            //    case 374817900273336321: //Pomona_Legendary_Raids
            //    case 366049983725830145: //EastLA_Raids
            //    case 374819488174178304: //EastLA_Legendary_Raids
            //        foreach (DiscordEmbed embed in message.Embeds)
            //        {
            //            if (embed.Description.Contains("Starbucks") ||
            //                embed.Description.Contains("Sprint"))
            //            {
            //                var webHook = "https://discordapp.com/api/webhooks/374830905547816960/qjSyb2EPRSdmKXOK2N_82nna8fZGAWHmUoLjBrxI5518Ua2OOcOGRpzKCltZqOA45wOh";
            //                await SendMessage(webHook, string.Empty, embed);
            //            }
            //        }
            //        break;
            //}
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

                foreach (var pokeId in user.PokemonIds)
                {
                    var pokemon = _db.Pokemon.Find(x => x.Index == pokeId);
                    if (pokemon == null) continue;

                    if (message.Author.Username.ToLower().Contains(pokemon.Name.ToLower()))
                    {
                        var msg = $"A wild {pokemon.Name} has appeared!\r\n\r\n" + message.Content;

                        Console.WriteLine($"Notifying user {discordUser.Username} that a {pokemon.Name} has appeared...");
                        Notify(discordUser, msg, pokemon, message.Embeds[0]);
                        await DirectMessage(discordUser, msg, message.Embeds.Count == 0 ? null : message.Embeds[0]);
                    }
                }
            }
        }

        private async Task DirectMessage(DiscordUser user, string message, DiscordEmbed embed)
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
            await DirectMessage
            (
                user,
                _config.WelcomeMessage.Replace("{username}", user.Username),
                //$"Hello {user.Username}, and welcome to versx's discord server!\r\n" +
                //"I am here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies.\r\n\r\n" +
                //"To see a full list of my available commands please send me a direct message containing `.help`.",
                null
            );
        }

        private async Task DisplaySettings()
        {
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
                        foreach (var pokeId in sub.PokemonIds)
                        {
                            Console.WriteLine(_db.Pokemon.Find(x => x.Index == pokeId).Name + $" ({pokeId})");
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
            Console.WriteLine($"********** {pokemon.Name} FOUND **********");
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