namespace PokeFilterBot
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Configuration;
    using PokeFilterBot.Data;
    using PokeFilterBot.Utilities;

    public class CommandProcessor
    {
        #region Variables

        private readonly Config _config;
        private readonly DiscordClient _client;
        private readonly Database _db;

        #endregion

        #region Constructor

        public CommandProcessor(Config config, DiscordClient client, Database db)
        {
            _config = config;
            _client = client;
            _db = db;
        }

        #endregion

        #region Public Methods

        public async Task ParseCommand(DiscordMessage message)
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
                case "create_roles":
                    if (isOwner) await ParseCreateRolesCommand(message);
                    break;
                case "delete_roles":
                    if (isOwner) await ParseDeleteRolesCommand(message);
                    break;
                case "restart":
                    break;
                case "shutdown":
                    if (isOwner) Environment.Exit(0);
                    break;
                default:
                    await message.RespondAsync("Invalid command, try sending me .help to see what available commands I can do.");
                    break;
            }

            _db.Save();
        }

        #endregion

        #region Private Methods

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

        #endregion

        #region Command Methods

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
                                if ((role.Name == "Valor" || role.Name == "Mystic" || role.Name == "Instinct") && role.Name != teamRole.Name)
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