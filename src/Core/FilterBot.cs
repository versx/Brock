namespace PokeFilterBot
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using PokeFilterBot.Configuration;
    using PokeFilterBot.Data;
    using PokeFilterBot.Utilities;

    public class FilterBot
    {
        #region Variables

        private DiscordClient _client;
        private readonly Database _db;
        private readonly Config _config;
        private CommandProcessor _processor;

        private readonly Random _rand;

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

            _processor = new CommandProcessor(_config, _client, _db);

            Console.WriteLine("Connecting to discord server...");
            await _client.ConnectAsync();

            await SendWelcomeMessage();

            //var embed = new DiscordEmbed()
            //{
            //    Title = "Sponsor Gym Test",
            //    Description = $"Starbucks coffee is good.",
            //    Color = 0xFF0000 // red
            //};

            //await SendMessage("https://discordapp.com/api/webhooks/366082287705784330/KsjTJ277pM9UPE_4KetoiBT3ZsQb6WUpryd47qvt006EF2197yRKO_yGTOduCoHvau3i", "Starbucks", embed);

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

        private async Task SendWelcomeMessage()
        {
            await SendMessage(_config.WebHookUrl, GetRandomWakeupMessage());
        }

        #endregion

        #region Discord Events

#pragma warning disable CS1998
        private async Task Client_Ready(ReadyEventArgs e)
        {
#pragma warning restore
            //await DisplaySettings();

            foreach (var user in _client.Presences)
            {
                Console.WriteLine($"User: {user.Key}: {user.Value.User.Username}");
            }
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            //Console.WriteLine($"Message recieved from server {e.Guild.Name} #{e.Message.Channel.Name}: {e.Message.Author.Username} (IsBot: {e.Message.Author.IsBot}) {e.Message.Content}");

            if (e.Message.Author.Id == _client.CurrentUser.Id) await Task.CompletedTask;

            if (e.Message.Author.IsBot)
            {
                await CheckSponsorRaids(e.Message);
                await CheckSubscriptions(e.Message);
            }
            else if (e.Message.Channel.Name == _config.CommandsChannel)
            {
                await _processor.ParseCommand(e.Message);
                //await ParseCommand(e.Message);
            }
        }

        private async Task Client_DmChannelCreated(DmChannelCreateEventArgs e)
        {
            var msg = await e.Channel.GetMessageAsync(e.Channel.LastMessageId);
            if (msg != null)
            {
                await _processor.ParseCommand(msg);
            }
        }

        #endregion

        #region Private Methods

        private async Task CheckSponsorRaids(DiscordMessage message)
        {
            foreach (DiscordEmbed embed in message.Embeds)
            {
                if (embed.Description.Contains("Starbucks") ||
                    embed.Description.Contains("Sprint"))
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
                            var webHook = "https://discordapp.com/api/webhooks/374830905547816960/qjSyb2EPRSdmKXOK2N_82nna8fZGAWHmUoLjBrxI5518Ua2OOcOGRpzKCltZqOA45wOh";
                            await SendMessage(webHook, string.Empty, embed);
                            break;
                    }
                }
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

                if (!user.Channels.Contains(message.Channel.Name)) continue;

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

        private string GetRandomWakeupMessage()
        {
            return _wakeupMessages[_rand.Next(0, _wakeupMessages.Length - 1)];
        }

        #endregion
    }
}