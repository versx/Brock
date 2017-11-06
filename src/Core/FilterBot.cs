namespace PokeFilterBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Timer = System.Timers.Timer;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using PokeFilterBot.Commands;
    using PokeFilterBot.Configuration;
    using PokeFilterBot.Data;
    using PokeFilterBot.Extensions;
    using PokeFilterBot.Utilities;

    //TODO: Notify via SMS or Twilio or w/e.

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
                    { "setup", new SetupCommand(_client, _db) },
                    { "remove", new RemoveCommand(_client, _db) },
                    { "sub", new SubscribeCommand(_db) },
                    { "unsub", new UnsubscribeCommand(_db) },
                    { "enable", new EnableDisableCommand(_db, true) },
                    { "disable", new EnableDisableCommand(_db, false) },
                    { "iam", new IamCommand(_client, _config) },
                    { "create_roles", new CreateRolesCommand(_client) },
                    { "delete_roles", new DeleteRolesCommand(_client) },
                    { "lobby", new CreateRaidLobbyCommand(_client, _db) },
                    { "checkin", new RaidLobbyCheckInCommand(_client, _db) },
                    { "ontheway", new RaidLobbyOnTheWayCommand(_client, _db) },
                    { "restart", new RestartCommand() },
                    { "shutdown", new ShutdownCommand() }
                };
            }

            if (_timer == null)
            {
                _timer = new Timer(5000);
#pragma warning disable RECS0165
                _timer.Elapsed += async (sender, e) =>
#pragma warning restore RECS0165
                {
                    if (_client == null) return;
                    foreach (var lobby in _db.Lobbies)
                    {
                        if (lobby.IsExpired)
                        {
                            var channel = await _client.GetChannelAsync(lobby.ChannelId);
                            if (channel == null)
                            {
                                Utils.LogError(new Exception($"Failed to find raid lobby channel {lobby.LobbyName} ({lobby.ChannelId})."));
                                return;
                            }
                            await channel.DeleteAsync($"Raid lobby {lobby.LobbyName} ({lobby.ChannelId}) no longer needed.");
                        }
                        await _client.UpdateLobbyStatus(lobby);
                    }
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

        #region Discord Events

        private async Task Client_Ready(ReadyEventArgs e)
        {
            await DisplaySettings();

            var randomWelcomeMessage = _wakeupMessages[_rand.Next(0, _wakeupMessages.Length - 1)];
            await SendMessage(_config.WebHookUrl, randomWelcomeMessage);

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            //Console.WriteLine($"Message recieved from server {e.Guild.Name} #{e.Message.Channel.Name}: {e.Message.Author.Username} (IsBot: {e.Message.Author.IsBot}) {e.Message.Content}");

            if (e.Message.Author.Id == _client.CurrentUser.Id) return;

            if (e.Message.Author.IsBot)
            {
                await CheckSponsorRaids(e.Message);
                await CheckSubscriptions(e.Message);
            }
            else if (e.Message.Channel.Name == _config.CommandsChannel)
            {
                await ParseCommand(e.Message);
            }
        }

        private async Task Client_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            var msg = await e.Channel.GetMessageAsync(e.Channel.LastMessageId);
            if (msg != null)
            {
                await ParseCommand(msg);
            }
        }

        private async Task Client_GuildBanAdded(GuildBanAddEventArgs e)
        {
            var channel = _client.GetChannelByName(_config.CommandsChannel);
            await channel.SendMessageAsync($"OH SNAP! The ban hammer was just dropped on {e.Member.Mention}, cya!");
        }

        private async Task Client_GuildBanRemoved(GuildBanRemoveEventArgs e)
        {
            var channel = _client.GetChannelByName(_config.CommandsChannel);
            await channel.SendMessageAsync($"Zeus was feeling nice today and unbanned {e.Member.Mention}, welcome back! Hopefully you'll learn to behave this time around.");
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            await SendBotIntroMessage(e.Member);

            var channel = _client.GetChannelByName(_config.CommandsChannel);
            await channel.SendMessageAsync($"Everyone let's welcome {e.Member.Mention} to the server! We've been waiting for you!");
        }

        private async Task Client_GuildMemberRemoved(GuildMemberRemoveEventArgs e)
        {
            var channel = _client.GetChannelByName(_config.CommandsChannel);
            await channel.SendMessageAsync($"Sorry to see you go {e.Member.Mention}, hope to see you back soon!");
        }

        #endregion

        #region Private Methods

        public async Task ParseCommand(DiscordMessage message)
        {
            var command = new Command(message.Content);
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
            switch (message.Channel.Id)
            {
                case 375047782827950092: //Legendary_Raids
                case 366049816188420096: //Upland_Raids
                case 374809552928899082: //Upland_Legendary_Raids
                case 366359725857832973: //Ontario_Raids
                case 374817863690747905: //Ontario_Legendary_Raids
                case 366049617642520596: //Pomona_Raids
                case 374817900273336321: //Pomona_Legendary_Raids
                case 366049983725830145: //EastLA_Raids
                case 374819488174178304: //EastLA_Legendary_Raids
                    foreach (DiscordEmbed embed in message.Embeds)
                    {
                        if (embed.Description.Contains("Starbucks") ||
                            embed.Description.Contains("Sprint"))
                        {
                            var webHook = "https://discordapp.com/api/webhooks/374830905547816960/qjSyb2EPRSdmKXOK2N_82nna8fZGAWHmUoLjBrxI5518Ua2OOcOGRpzKCltZqOA45wOh";
                            await SendMessage(webHook, string.Empty, embed);
                        }
                    }
                    break;
            }
        }

        private async Task CheckSubscriptions(DiscordMessage message)
        {
            DiscordUser discordUser;
            foreach (var user in _db.Subscriptions)
            {
                if (!user.Enabled) continue;

                discordUser = await _client.GetUserAsync(user.UserId);
                if (discordUser == null) continue;

                if (!user.Channels.Contains(message.Channel.Id)) continue;

                foreach (var pokeId in user.PokemonIds)
                {
                    var pokemon = _db.Pokemon.Find(x => x.Index == pokeId);
                    if (pokemon == null) continue;

                    if (string.Compare(message.Author.Username, pokemon.Name, true) == 0)
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

            DiscordGuild guild = await _client.GetGuildAsync(guildId);
            if (guild == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Guild does not exist!");
                Console.ResetColor();
                return;
            }
            //DiscordChannel channel = guild.GetChannel(channelId);
            DiscordChannel channel = await _client.GetChannelAsync(channelId);
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
                $"Hello {user.Username}, and welcome to versx's discord server!\r\n" +
                "I am here to help you with certain things if you require them such as notifications of Pokemon that have spawned as well as setting up Raid Lobbies.\r\n\r\n" +
                "To see a full list of my available commands please send me a direct message containing `.help`.",
                null
            );
        }

        private async Task DisplaySettings()
        {
            var owner = await _client.GetUserAsync(_config.OwnerId);
            Console.WriteLine($"Owner: {owner?.Username} ({_config.OwnerId})");
            Console.WriteLine($"Authentication Token: {_config.AuthToken}");
            Console.WriteLine($"Commands Channel: {_config.CommandsChannel}");
            Console.WriteLine($"Startup WebHook: {_config.WebHookUrl}");
            Console.WriteLine();
            Console.WriteLine("Current Subscriptions:");
            Console.WriteLine();
            foreach (var sub in _db.Subscriptions)
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
                    Console.WriteLine($"Channel Subscriptions:");
                    foreach (var channel in sub.Channels)
                    {
                        Console.WriteLine("#" + channel);
                    }
                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
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