namespace BrockBot.Services.RaidLobby
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;
    using DSharpPlus.EventArgs;

    using BrockBot.Configuration;
    using BrockBot.Data.Models;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    public class RaidLobbyManager
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public List<string> ValidRaidEmojis => new List<string>
        {
            "➡",
            "✅",
            "❌",
            "1⃣",
            "2⃣",
            "3⃣",
            "4⃣",
            "5⃣",
            "🔟",
            "🔄"
        };

        #endregion

        #region Constructor

        public RaidLobbyManager(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task ProcessReaction(MessageReactionAddEventArgs e)
        {
            if (!ValidRaidEmojis.Contains(e.Emoji.Name)) return;

            if (e.User.IsBot) return;

            if (e.Channel.Guild == null)
            {
                await ProcessRaidLobbyReactionDM(e.User, e.Channel, e.Message, e.Emoji);
            }
            else
            {
                await ProcessRaidLobbyReaction(e.User, e.Channel, e.Message, e.Emoji);
            }
        }

        public async Task<bool> DeleteExpiredRaidLobby(ulong originalMessageId)
        {
            _logger.Trace($"FilterBot::DeleteExpiredRaidLobby [OriginalMessageId={originalMessageId}]");

            if (!_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId)) return false;

            var lobby = _config.RaidLobbies.ActiveLobbies[originalMessageId];
            var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                _logger.Error($"Failed to find raid lobby channel with id {_config.RaidLobbies.RaidLobbiesChannelId}, does it exist?");
                return false;
            }

            var lobbyMessage = await raidLobbyChannel.GetMessage(lobby.LobbyMessageId);
            if (lobbyMessage == null)
            {
                _logger.Error($"Failed to find raid lobby message with id {lobby.LobbyMessageId}, must have already been deleted.");
                return true;
            }

            try
            {
                await lobbyMessage.DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return false;
        }

        #endregion

        #region Private Methods

        private async Task ProcessRaidLobbyReactionDM(DiscordUser user, DiscordChannel channel, DiscordMessage message, DiscordEmoji emoji)
        {
            var hasPrivilege = await _client.IsSupporterOrHigher(user.Id, _config);
            if (!hasPrivilege)
            {
                await message.RespondAsync($"{user.Username} does not have the supporter role assigned.");
                return;
            }

            var origMessageId = Convert.ToUInt64(Utils.GetBetween(message.Content, "#", "#"));
            var lobby = GetLobby(channel, ref origMessageId);

            var settings = await GetRaidLobbySettings(lobby, origMessageId, message, channel);
            if (settings == null)
            {
                _logger.Error($"Failed to find raid lobby settings for original raid message id {origMessageId}.");
                return;
            }

            await message.DeleteReactionAsync(emoji, user);

            var lobMessage = default(DiscordMessage);
            var embedMsg = settings.RaidMessage?.Embeds[0];

            switch (emoji.Name)
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
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Five;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
                    await message.DeleteAllReactionsAsync();
                    break;
                case "🔟":
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Ten;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
                    await message.DeleteAllReactionsAsync();
                    break;
                case "❌":
                    if (!lobby.UsersComing.ContainsKey(user.Id))
                    {
                        lobby.UsersComing.Remove(user.Id);
                        lobMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embedMsg);
                    }
                    break;
            }
            _config.RaidLobbies.ActiveLobbies[origMessageId] = lobby;
            _config.Save();
        }

        private async Task ProcessRaidLobbyReaction(DiscordUser user, DiscordChannel channel, DiscordMessage message, DiscordEmoji emoji)
        {
            var result = _config.SponsoredRaids.Exists(x => x.ChannelPool.Contains(channel.Id)) ||
                         _config.RaidLobbies.RaidLobbiesChannelId == channel.Id;

            if (!result) return;

            var hasPrivilege = await _client.IsSupporterOrHigher(user.Id, _config);
            if (!hasPrivilege)
            {
                await message.RespondAsync($"{user.Username} does not have the supporter role assigned.");
                return;
            }

            var originalMessageId = message.Id;
            var lobby = GetLobby(channel, ref originalMessageId);

            var settings = await GetRaidLobbySettings(lobby, originalMessageId, message, channel);
            if (settings == null)
            {
                _logger.Error($"Failed to find raid lobby settings for original raid message id {originalMessageId}.");
                return;
            }

            await message.DeleteAllReactionsAsync();

            var lobbyMessage = default(DiscordMessage);
            var embed = settings.RaidMessage?.Embeds[0];

            switch (emoji.Name)
            {
                case "➡":
                    #region Coming
                    if (!lobby.UsersComing.ContainsKey(user.Id))
                    {
                        lobby.UsersComing.Add(user.Id, new RaidLobbyUser { Id = user.Id, Eta = RaidLobbyEta.NotSet, Players = 1 });
                    }

                    if (lobby.UsersReady.ContainsKey(user.Id))
                    {
                        lobby.UsersReady.Remove(user.Id);
                    }

                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetAccountsReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "✅":
                    #region Ready
                    if (!lobby.UsersReady.ContainsKey(user.Id))
                    {
                        var players = lobby.UsersComing.ContainsKey(user.Id) ? lobby.UsersComing[user.Id].Players : 1;
                        lobby.UsersReady.Add(user.Id, new RaidLobbyUser { Id = user.Id, Eta = RaidLobbyEta.Here, Players = players });
                    }

                    if (lobby.UsersComing.ContainsKey(user.Id))
                    {
                        lobby.UsersComing.Remove(user.Id);
                    }

                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbies.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "❌":
                    #region Remove User From Lobby
                    if (lobby.UsersComing.ContainsKey(user.Id)) lobby.UsersComing.Remove(user.Id);
                    if (lobby.UsersReady.ContainsKey(user.Id)) lobby.UsersReady.Remove(user.Id);

                    if (lobby.UsersComing.Count == 0 && lobby.UsersReady.Count == 0)
                    {
                        lobbyMessage = await settings.RaidLobbyChannel.GetMessage(lobby.LobbyMessageId);
                        if (lobbyMessage != null)
                        {
                            await lobbyMessage.DeleteAsync();
                            lobbyMessage = null;
                        }

                        _config.RaidLobbies.ActiveLobbies.Remove(lobby.OriginalRaidMessageId);
                    }
                    break;
                #endregion
                case "1⃣":
                    #region 1 Account
                    lobby.UsersComing[user.Id].Players = 1;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "2⃣":
                    #region 2 Accounts
                    lobby.UsersComing[user.Id].Players = 2;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "3⃣":
                    #region 3 Accounts
                    lobby.UsersComing[user.Id].Players = 3;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "4⃣":
                    #region 4 Accounts
                    lobby.UsersComing[user.Id].Players = 4;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetEtaReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message
                    );
                    break;
                #endregion
                case "5⃣":
                    #region 5mins ETA
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Five;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbies.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "🔟":
                    #region 10mins ETA
                    lobby.UsersComing[user.Id].Eta = RaidLobbyEta.Ten;
                    lobby.UsersComing[user.Id].EtaStart = DateTime.Now;
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    if (_config.RaidLobbies.RaidLobbiesChannelId == channel.Id)
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                    }
                    else
                    {
                        await _client.SetDefaultRaidReactions(lobbyMessage, true);
                        await _client.SetDefaultRaidReactions(message, false);
                    }
                    break;
                #endregion
                case "🔄":
                    #region Refresh
                    lobbyMessage = await UpdateRaidLobbyMessage(lobby, settings.RaidLobbyChannel, embed);
                    await _client.SetDefaultRaidReactions
                    (
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                        ? lobbyMessage
                        : message,
                        _config.RaidLobbies.RaidLobbiesChannelId == channel.Id
                    );
                    break;
                    #endregion
            }
            if (lobby != null)
            {
                if (_config.RaidLobbies.ActiveLobbies.ContainsKey(originalMessageId))
                {
                    _config.RaidLobbies.ActiveLobbies[originalMessageId] = lobby;
                }
            }
            _config.Save();
        }

        #endregion

        #region Raid Lobby

        private RaidLobby GetLobby(DiscordChannel channel, ref ulong originalMessageId)
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

            return lobby;
        }

        private async Task<RaidLobbySettings> GetRaidLobbySettings(RaidLobby lobby, ulong originalMessageId, DiscordMessage message, DiscordChannel channel)
        {
            _logger.Trace($"FilterBot::GetRaidLobbySettings [OriginalMessageId={originalMessageId}, DiscordMessage={message.Content}, DiscordChannel={channel.Name}]");

            var raidLobbyChannel = await _client.GetChannel(_config.RaidLobbies.RaidLobbiesChannelId);
            if (raidLobbyChannel == null)
            {
                _logger.Error($"Failed to retrieve the raid lobbies channel with id {_config.RaidLobbies.RaidLobbiesChannelId}.");
                return null;
            }

            if (lobby == null)
            {
                _logger.Error($"Failed to find raid lobby, it may have already expired, deleting message with id {message.Id}...");
                await message.DeleteAsync("Raid lobby does not exist anymore.");
                return null;
            }

            var origChannel = await _client.GetChannel(lobby.OriginalRaidMessageChannelId);
            if (origChannel == null)
            {
                _logger.Error($"Failed to find original raid message channel with id {lobby.OriginalRaidMessageChannelId}.");
                return null;
            }

            var raidMessage = await origChannel.GetMessage(originalMessageId);
            if (raidMessage == null)
            {
                _logger.Warn($"Failed to find original raid message with {originalMessageId}, searching server...");
                raidMessage = await GetRaidMessage(_config.SponsoredRaids, originalMessageId);
            }

            _config.Save();

            return new RaidLobbySettings
            {
                OriginalRaidMessageChannel = origChannel,
                RaidMessage = raidMessage,
                RaidLobbyChannel = raidLobbyChannel
            };
        }

        private async Task<DiscordMessage> UpdateRaidLobbyMessage(RaidLobby lobby, DiscordChannel raidLobbyChannel, DiscordEmbed raidMessage)
        {
            _logger.Trace($"FilterBot::UpdateRaidLobbyMessage [RaidLobby={lobby.LobbyMessageId}, DiscordChannel={raidLobbyChannel.Name}, DiscordMessage={raidMessage.Title}]");

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
                _logger.Error($"Failed to set default raid reactions to message {lobby.LobbyMessageId}, couldn't find message...");
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
                _logger.Error(ex);
                return 0;
            }
        }

        private async Task<DiscordMessage> GetRaidMessage(List<SponsoredRaidsConfig> sponsoredRaids, ulong messageId)
        {
            _logger.Trace($"FilterBot::GetRaidMessage [SponsoredRaids={sponsoredRaids.Count}, MessageId={messageId}]");

            foreach (var sponsored in sponsoredRaids)
            {
                foreach (var channelId in sponsored.ChannelPool)
                {
                    var channel = await _client.GetChannel(channelId);
                    if (channel == null)
                    {
                        _logger.Error($"Failed to find channel {channelId}.");
                        continue;
                    }

                    var message = await channel.GetMessage(messageId);
                    if (message == null) continue;

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
                    _logger.Error($"Failed to find discord user with id {item.Key}.");
                    continue;
                }

                //TODO: Fix Eta countdown.
                var timeLeft = TimeLeft(item.Value.EtaStart);
                if (timeLeft == 0)
                {
                    if (item.Value.Eta != RaidLobbyEta.NotSet && item.Value.Eta != RaidLobbyEta.Here)
                    {
                        item.Value.Eta = RaidLobbyEta.Late;
                    }
                    ////User is late, send DM.
                    //var dm = await _client.SendDirectMessage(user, $"{user.Mention} you're late for the raid, do you want to extend your time? If not please click the red cross button below to remove yourself from the raid lobby.\r\n#{item.Key}#", null);
                    //if (dm == null)
                    //{
                    //    Logger.Error($"Failed to send {user.Username} a direct message letting them know they are late for the raid.");
                    //    continue;
                    //}

                    //await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":five:"));
                    //await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":keycap_ten:"));
                    //await dm.CreateReactionAsync(DiscordEmoji.FromName(_client, ":x:"));
                }

                var eta = (item.Value.Eta != RaidLobbyEta.Here && item.Value.Eta != RaidLobbyEta.NotSet && item.Value.Eta != RaidLobbyEta.Late ? $"{timeLeft} minute{(timeLeft > 1 ? "s" : null)}" : item.Value.Eta.ToString());
                list.Add($"{user.Username} ({item.Value.Players} account{(item.Value.Players == 1 ? "" : "s")}, ETA: {eta})");
            }
            return list;
        }

        #endregion
    }
}