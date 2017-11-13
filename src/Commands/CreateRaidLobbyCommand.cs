namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Extensions;
    using BrockBot.Utilities;

    //TODO: Create raid lobbies with user wait time, amount of users in lobby.
    /*
     * .lobby test 39848234234
     * .join lobby test
     * bot creates lobby channel with raid message id
     * bot pins raid start, end, raid info, gym info, directions message to lobby channel
     * user checks in with .checkin command when at raid
     * user uses .ontheway 5mins eta command
     * keep track of the amount of time users have been checked in and count down how long eta users are expected.
     */

    /**Example Usage:
     * .lobby TyranitarWestFontana 2389498237482374
     */
    public class CreateRaidLobbyCommand : ICustomCommand
    {
        private const string ActiveRaidLobbies = "ACTIVE RAID LOBBIES";

        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public CreateRaidLobbyCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 2)
            {
                var lobbyName = command.Args[0];
                var raidMessageId = Convert.ToUInt64(command.Args[1]);

                var raidMessage = await _client.GetMessageById(message.Channel.GuildId, raidMessageId);
                if (raidMessage == null)
                {
                    await message.RespondAsync($"Failed to find a message matching the provided message id '{raidMessageId}.");
                    return;
                }

                var category = _client.GetChannelByName(ActiveRaidLobbies);
                if (category == null)
                {
                    category = await message.Channel.Guild.CreateChannelAsync(ActiveRaidLobbies, ChannelType.Category);
                }

                /**
Available Until: 06:49:07pm (46m 24s)
Where: unknown gym.
3044 1/2 E 4th St Los Angeles, CA 90063
unknown

Quick Move: Iron Tail (DPS: 13.64, Damage: 15)
Charge Move: Fire Blast (DPS: 33.33, Damage: 140)
34.036126,-118.201974
                 */

                var lobbyChannel = _client.GetChannelByName(lobbyName);
                if (lobbyChannel == null)
                {
                    lobbyChannel = await message.Channel.Guild.CreateChannelAsync(lobbyName, ChannelType.Text, category);
                }

                var raidMsgEmbed = raidMessage.Embeds[0];
                var content = raidMsgEmbed.Description.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                var expires = Utils.GetBetween(content[0], "Available Until: ", " (");
                var expireTime = DateTime.Parse(expires);
                var remainingTime = Utils.GetBetween(content[0], "(", ")");
                var gymName = Utils.GetBetween(content[1], "Where: ", " gym.");
                var address = content[2];
                var pokemon = raidMessage.Author.Username.Replace(" Raid", null);

                var lobby = new RaidLobby
                {
                    Address = address,
                    ChannelId = lobbyChannel.Id,
                    ExpireTime = expireTime,
                    GymName = gymName,
                    LobbyName = lobbyName,
                    OriginalRaidMessageId = raidMessage.Id,
                    PokemonName = pokemon,
                    StartTime = expireTime - TimeSpan.FromMinutes(45),
                };

                if (message.Channel == null) return;
                var server = _db[message.Channel.GuildId];
                if (server == null) return;

                if (!server.Lobbies.Contains(lobby))
                {
                    server.Lobbies.Add(lobby);
                }

                await message.RespondAsync($"Raid lobby {lobbyName} was created successfully.");

                await _client.SendLobbyStatus(lobby, raidMessage.Embeds[0], true);
            }
        }
    }
}