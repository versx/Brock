namespace PokeFilterBot
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using Newtonsoft.Json;

    using PokeFilterBot.Configuration;
    using PokeFilterBot.Data;
    using PokeFilterBot.Utilities;

    public class FilterBot
    {
        #region Variables

        private DiscordClient _client;
        private readonly Database _db;
        private readonly Config _config;

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
            var data = GetWebHookData(webHookUrl);
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

        private dynamic GetWebHookData(string webHook)
        {
            /**Example:
             * {
             *   "name": "Pogo", 
             *   "channel_id": "352137087182416016", 
             *   "token": "fCdHsCZWeGB_vTkdPRqnB4_7fXil5tutXDLAZQYDurkXWQOqzSptiSQHbiCOBGlsF8J8", 
             *   "avatar": null, 
             *   "guild_id": "322025055510855680", 
             *   "id": "352156775101032449"
             * }
             * 
             */

            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                string json = wc.DownloadString(webHook);
                dynamic data = JsonConvert.DeserializeObject(json);
                return data;
            }
        }

        private List<string> GetSubscriptionNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.Subscriptions.ContainsKey(userId))
            {
                var pokeIds = _db.Subscriptions[userId].PokemonIds;
                pokeIds.Sort();

                foreach (uint id in pokeIds)
                {
                    var pokemon = _db.Pokemon.Find(x => x.Index == id);
                    if (pokemon == null) continue;

                    list.Add(pokemon.Name);
                }
            }
            return list;
        }

        private List<string> GetChannelNames(ulong userId)
        {
            var list = new List<string>();
            if (_db.Subscriptions.ContainsKey(userId))
            {
                var channelIds = _db.Subscriptions[userId].Channels;
                //channelIds.Sort();
                return channelIds;

                //foreach (string channel in channelIds)
                //{
                //    var channel = await _client.GetChannelAsync(id);
                //    if (channel == null) continue;

                //    list.Add(channel);
                //}
            }
            //list.Sort();
            return list;
        }

        private DiscordChannel GetChannelByName(string channelName)
        {
            foreach (var guild in _client.Guilds)
            {
                foreach (var channel in guild.Value.Channels)
                {
                    if (channel.Name == channelName)
                    {
                        return channel;
                    }
                }
            }

            return null;
        }

        private string GetRandomWakeupMessage()
        {
            return _wakeupMessages[_rand.Next(0, _wakeupMessages.Length - 1)];
        }

        #endregion

        #region Parse Commands

        private async Task ParseCommand(DiscordMessage message)
        {
            var command = new Command(message.Content);
            if (!command.ValidCommand && !message.Author.IsBot) return;
            var isOwner = message.Author.Id == _config.OwnerId;

            switch (command.Name)
            {
                case "demo":
                    await ParseDemoCommand(message);
                    break;
                case "help":
                    await ParseHelpCommand(message);
                    break;
                case "info":
                    await ParseInfoCommand(message);
                    break;
                case "v":
                case "ver":
                case "version":
                    await ParseVersionCommand(message);
                    break;
                case "setup":
                    await ParseSetupCommand(message, command);
                    break;
                case "remove":
                    await ParseRemoveCommand(message, command);
                    break;
                case "sub":
                    await ParseSubscribeCommand(message, command);
                    break;
                case "unsub":
                    await ParseUnsubscribeCommand(message, command);
                    break;
                case "enable":
                    await ParseEnableDisableCommand(message, true);
                    break;
                case "disable":
                    await ParseEnableDisableCommand(message, false);
                    break;
                case "iam":
                    await ParseTeamAssignmentCommand(message, command);
                    break;
                default:
                    await message.RespondAsync("Invalid command, try sending me .help to see what available commands I can do.");
                    break;
            }

            if (isOwner)
            {
                switch (command.Name)
                {
                    case "create_roles":
                        await ParseCreateRolesCommand(message);
                        break;
                    case "delete_roles":
                        await ParseDeleteRolesCommand(message);
                        break;
                    case "restart":
                        break;
                    case "shutdown":
                        Environment.Exit(0);
                        break;
                }
            }

            _db.Save();
        }

        private async Task ParseDemoCommand(DiscordMessage message)
        {
            await message.RespondAsync("Demo command not yet implemented.");
        }

        private async Task ParseVersionCommand(DiscordMessage message)
        {
            await message.RespondAsync
            (
                $"{AssemblyUtils.AssemblyName} Version: {AssemblyUtils.AssemblyVersion}\r\n" +
                $"Created by: {AssemblyUtils.CompanyName}\r\n" +
                $"{AssemblyUtils.Copyright}"
            );
        }

        private async Task ParseSetupCommand(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 1)
            {
                var author = message.Author.Id;
                foreach (var chlName in command.Args[0].Split(','))
                {
                    var channelName = chlName;
                    if (channelName[0] == '#') channelName = channelName.Remove(0, 1);

                    var channel = GetChannelByName(channelName);
                    if (channel == null)
                    {
                        await message.RespondAsync($"Channel name {channelName} is not a valid channel.");
                        continue;
                    }

                    if (!_db.Subscriptions.ContainsKey(author))
                    {
                        _db.Subscriptions.Add(new Subscription(author, new List<uint>(), new List<string> { channel.Name }));
                        await message.RespondAsync($"You have successfully subscribed to #{channel.Name} notifications!");
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!_db.Subscriptions[author].Channels.Contains(channel.Name))
                        {
                            _db.Subscriptions[author].Channels.Add(channel.Name);
                            await message.RespondAsync($"You have successfully subscribed to #{channel.Name} notifications!");
                        }
                        else
                        {
                            await message.RespondAsync($"You are already subscribed to #{channel.Name} notifications.");
                        }
                    }
                }
            }
        }

        private async Task ParseRemoveCommand(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 1)
            {
                var author = message.Author.Id;
                foreach (var chlName in command.Args[0].Split(','))
                {
                    var channelName = chlName;
                    if (channelName[0] == '#') channelName = channelName.Remove(0, 1);

                    var channel = GetChannelByName(channelName);
                    if (channel == null)
                    {
                        await message.RespondAsync($"Channel name {channelName} is not a valid channel.");
                        continue;
                    }

                    if (!_db.Subscriptions.ContainsKey(author))
                    {
                        await message.RespondAsync($"You are not currently subscribed to any Pokemon notifications from any channels.");
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (_db.Subscriptions[author].Channels.Contains(channel.Name))
                        {
                            if (_db.Subscriptions[author].Channels.Remove(channel.Name))
                            {
                                await message.RespondAsync($"You have successfully unsubscribed from #{channel.Name} Pokemon notifications.");
                            }
                        }
                        else
                        {
                            await message.RespondAsync($"You are not currently subscribed to any #{channel.Name} Pokemon notifications.");
                        }
                    }
                }
            }
        }

        private async Task ParseSubscriptionsCommand(DiscordMessage message)
        {
            var author = message.Author.Id;
            var subsMsg = _db.Subscriptions.ContainsKey(author)
                ? _db.Subscriptions[author].PokemonIds.Count == 0
                    ? "You are not currently subscribed to any Pokemon notifications."
                    : "You are currently subscribed to " + string.Join(", ", GetSubscriptionNames(author)) + " notifications."
                : "You are not subscribed to any Pokemon.";
            await message.RespondAsync(subsMsg);
        }

        private async Task ParseSubscribeCommand(DiscordMessage message, Command command)
        {
            //notify <pkmn> <min_cp> <min_iv>
            var author = message.Author.Id;
            if (command.HasArgs && command.Args.Count == 1)
            {
                foreach (var arg in command.Args[0].Split(','))
                {
                    var index = Convert.ToUInt32(arg);
                    var pokemon = _db.Pokemon.Find(x => x.Index == index);
                    if (pokemon == null)
                    {
                        await message.RespondAsync($"Pokedex number {index} is not a valid Pokemon id.");
                        continue;
                    }

                    if (!_db.Subscriptions.ContainsKey(author))
                    {
                        _db.Subscriptions.Add(new Subscription(author, new List<uint> { index }, new List<string>()));
                        await message.RespondAsync($"You have successfully subscribed to {pokemon.Name} notifications!");
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (!_db.Subscriptions[author].PokemonIds.Contains(index))
                        {
                            _db.Subscriptions[author].PokemonIds.Add(index);
                            await message.RespondAsync($"You have successfully subscribed to {pokemon.Name} notifications!");
                        }
                        else
                        {
                            await message.RespondAsync($"You are already subscribed to {pokemon.Name} notifications.");
                        }
                    }
                }
            }
        }

        private async Task ParseUnsubscribeCommand(DiscordMessage message, Command command)
        {
            var author = message.Author.Id;

            if (_db.Subscriptions.ContainsKey(author))
            {
                if (command.HasArgs && command.Args.Count == 1)
                {
                    foreach (var arg in command.Args[0].Split(','))
                    {
                        var index = Convert.ToUInt32(arg);
                        var pokemon = _db.Pokemon.Find(x => x.Index == index);
                        if (pokemon == null)
                        {
                            await message.RespondAsync($"Pokedex number {index} is not a valid Pokemon id.");
                            continue;
                        }

                        if (_db.Subscriptions[author].PokemonIds.Contains(index))
                        {
                            if (_db.Subscriptions[author].PokemonIds.Remove(index))
                            {
                                await message.RespondAsync($"You have successfully unsubscribed from {pokemon.Name} notifications!");
                            }
                        }
                        else
                        {
                            await message.RespondAsync($"You are not subscribed to {pokemon.Name} notifications.");
                        }
                    }
                }
                else
                {
                    _db.Subscriptions.Remove(author);
                    await message.RespondAsync($"You have successfully unsubscribed from all notifications!");
                }
            }
            else
            {
                await message.RespondAsync($"You are not subscribed to any notifications.");
            }
        }

        private async Task ParseEnableDisableCommand(DiscordMessage message, bool enable)
        {
            var author = message.Author.Id;
            if (_db.Subscriptions.ContainsKey(author))
            {
                _db.Subscriptions[author].Enabled = enable;
                await message.RespondAsync($"You have {(enable ? "" : "de-")}activated Pokemon notifications.");
            }
        }

        private async Task ParseTeamAssignmentCommand(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 1)
            {
                var team = command.Args[0];
                switch (team)
                {
                    case "Valor":
                    case "valor":
                    case "Mystic":
                    case "mystic":
                    case "Instinct":
                    case "instinct":
                    case "None":
                    case "none":
                        try
                        {
                            var member = await GetMemberFromUserId(message.Author.Id);
                            var teamRole = GetRoleFromName(team);
                            var reason = "User initiated team assignment via PokeFilterBot.";
                            //TODO: Only retrieve the current guild.
                            if (message.Channel.Guild == null)
                            {
                                //TODO: Ask what server to assign to.
                                //foreach (var guild in _client.Guilds)
                                //{
                                //    await guild.Value.GrantRoleAsync(member, teamRole, reason);
                                //}
                                await message.RespondAsync($"Currently I only support team assignment via the channel #{_config.CommandsChannel}, direct message support is coming soon.");
                                return;
                            }

                            foreach (var role in member.Roles)
                            {
                                if (role.Name == "Valor" || role.Name == "Mystic" || role.Name == "Instinct")
                                {
                                    await message.Channel.Guild.RevokeRoleAsync(member, role, reason);
                                    await message.RespondAsync($"{message.Author.Username} has left team {role.Name}.");
                                }
                            }

                            if (teamRole != null)
                            {
                                await message.Channel.Guild.GrantRoleAsync(member, teamRole, reason);
                                await message.RespondAsync($"{message.Author.Username} has joined team {teamRole.Name}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"ERROR: {ex}");
                        }
                        break;
                    default:
                        await message.RespondAsync($"You have entered an incorrect team name, please enter one of the following: Valor, Mystic, or Instinct.");
                        break;
                }
            }
        }

        private DiscordRole GetRoleFromName(string roleName)
        {
            foreach (var guild in _client.Guilds)
            {
                foreach (var role in guild.Value.Roles)
                {
                    if (string.Compare(role.Name, roleName, true) == 0)
                    {
                        return role;
                    }
                }
            }

            return null;
        }

        private async Task<DiscordMember> GetMemberFromUserId(ulong userId)
        {
            foreach (var guild in _client.Guilds)
            {
                var user = await guild.Value.GetMemberAsync(userId);
                if (user != null)
                {
                    return user;
                }
            }

            return null;
        }

        private async Task ParseInfoCommand(DiscordMessage message)
        {
            var author = message.Author.Id;
            var isSubbed = _db.Subscriptions.ContainsKey(author);
            var hasPokemon = isSubbed && _db.Subscriptions[author].PokemonIds.Count > 0;
            var hasChannels = isSubbed && _db.Subscriptions[author].Channels.Count > 0;
            var msg = string.Empty;

            if (isSubbed)
            {
                if (hasPokemon && hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(author))} notifications from channels #{string.Join(", #", GetChannelNames(author))}.";
                }
                else if (hasPokemon && !hasChannels)
                {
                    msg = $"You are currently subscribed to {string.Join(", ", GetSubscriptionNames(author))} notifications from zero channels.";
                }
                else if (!hasPokemon && hasChannels)
                {
                    msg = $"You are not currently subscribed to any Pokemon notifications from channels #{string.Join(", #", GetChannelNames(author))}.";
                }
                else if (!hasPokemon && !hasChannels)
                {
                    msg = "You are not currently subscribed to any Pokemon notifications from any channels.";
                }
            }
            else
            {
                msg = "You are not subscribed to any Pokemon.";
            }

            await message.RespondAsync(msg);
        }

        private async Task ParseHelpCommand(DiscordMessage message)
        {
            await message.RespondAsync
            (
                $".info - Shows the your current Pokemon subscriptions and which channels to listen to.\r\n\r\n" +
                ".setup - Include Pokemon from the specified channels to be notified of.\r\n" +
                    "\tExample: .setup channel1,channel2\r\n" +//34293948729384,3984279823498\r\n" + 
                    "\tExample: .setup channel1\r\n\r\n" +//34982374982734\r\n" +
                ".remove - Removes the selected channels from being notified of Pokemon.\r\n" +
                    "\tExample: .remove channel1,channel2\r\n" +
                    "\tExample: .remove single_channel1\r\n\r\n" +
                //".subs - Lists all Pokemon subscriptions.\r\n" +
                ".sub - Subscribe to Pokemon notifications via pokedex number.\r\n" +
                    "\tExample: .sub 147.\r\n" +
                    "\tExample: .sub 113,242,248\r\n\r\n" +
                ".unsub - Unsubscribe from a single or multiple Pokemon notification or even all subscribed Pokemon notifications.\r\n" +
                    "\tExample: .unsub 149\r\n" +
                    "\tExample: .unsub 3,6,9,147,148,149\r\n" +
                    "\tExample: .unsub (Removes all subscribed Pokemon notifications.)\r\n\r\n" +
                ".enable - Activates the Pokemon notification subscriptions.\r\n" +
                ".disable - Deactivates the Pokemon notification subscriptions.\r\n\r\n" +
                $".demo - Display a demo of the {AssemblyUtils.AssemblyName}.\r\n" +
                $".v, .ver, or .version - Display the current {AssemblyUtils.AssemblyName} version.\r\n\r\n" +
                $"If you are the owner of the bot you can execute the following additional commands:\r\n" + 
                ".create_roles - Creates the required team roles to be assigned when users type the .iam <team> commmand.\r\n" +
                ".delete_roles - Deletes all team roles that the PokeFilterBot created.\r\n" + 
                ".help - Shows this help message."
            );
        }

        private async Task ParseCreateRolesCommand(DiscordMessage message)
        {
            var teams = new[] { "Valor", "Mystic", "Instinct" };
            var colors = new[] { DiscordColor.Red, DiscordColor.Blue, DiscordColor.Yellow };

            for (int i = 0; i < teams.Length; i++)
            {
                try
                {
                    if (GetRoleFromName(teams[i]) == null)
                    {
                        if (message.Channel.Guild != null)
                        {
                            await message.Channel.Guild.CreateRoleAsync(teams[i], message.Channel.Guild.EveryoneRole.Permissions, colors[i], null, true, null);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Utils.LogError(ex);
                    await message.RespondAsync($"Failed to create team role {teams[i]}, it might already exist or I do not have the correct permissions to manage roles.");
                }
            }

            await message.RespondAsync("Valor, Mystic, and Instinct team roles were successfully created.");
        }

        private async Task ParseDeleteRolesCommand(DiscordMessage message)
        {
            try
            {
                foreach (var role in message.Channel.Guild.Roles)
                {
                    switch (role.Name)
                    {
                        case "Valor":
                        case "Mystic":
                        case "Instinct":
                            await message.Channel.Guild.DeleteRoleAsync(role);
                            break;
                    }
                }

                await message.RespondAsync("All team roles have been deleted.");
            }
            catch (Exception ex)
            {
                Utils.LogError(ex);
                await message.RespondAsync("Failed to delete one or more team roles.");
            }
        }

        #endregion
    }
}