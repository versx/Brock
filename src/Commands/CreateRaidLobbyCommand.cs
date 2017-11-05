namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;
    using PokeFilterBot.Data.Models;
    using PokeFilterBot.Extensions;
    using PokeFilterBot.Utilities;

    //TODO: Create raid lobbies with user wait time, amount of users in lobby, when the raid starts/ends, the name and description, raid info etc.
    /*
     * .lobby test 39848234234
     * .join lobby test
     * bot creates lobby channel with raid message id
     * bot pins raid start, end, raid info, gym info, directions message to lobby channel
     * user checks in with .checkin command when at raid
     * user uses .ontheway 5mins eta command
     * keep track of the amount of time users have been checked in and count down how long eta users are expected.
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
                var raidMessage = await _client.GetMessageById(raidMessageId);
                if (raidMessage == null)
                {
                    await message.RespondAsync($"Failed to find a message matching the provided message id '{raidMessageId}.");
                    await Task.CompletedTask;
                }

                var category = _client.GetChannelByName(ActiveRaidLobbies);
                if (category == null)
                {
                    category = await message.Channel.Guild.CreateChannelAsync(ActiveRaidLobbies, ChannelType.Category);
                }

                var lobby = new RaidLobby(lobbyName, raidMessageId); //lobby channel id.
                //lobby.PokemonName = raidMessage.Embeds[0].Author.Name;
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
                var expireTime = Utils.GetBetween(content[0], "Available Until: ", " (");
                var remainingTime = Utils.GetBetween(content[1], "(", ")");
                var gymName = Utils.GetBetween(content[1], "Where: ", " gym.");
                var address = content[2];
                var description = content[3];
                //4 = empty
                var quickMoveInfo = content[5];
                var chargeMoveInfo = content[6];
                var coordinates = content[7];
                var pokemon = raidMessage.Author.Username; //Includes " Raid" part.

                var raidInfoMessage = await lobbyChannel.SendMessageAsync
                (
                    $"# Expire Time: {expireTime}\r\n" +
                    $"Gym Name: {gymName}\r\n" +
                    $"Address: {address}\r\n" +
                    $"Description: {description}\r\n" +
                    $"Raid Boss Quick Move: {quickMoveInfo}\r\n" +
                    $"Raid Boss Charge Move: {chargeMoveInfo}\r\n" +
                    $"Co-ordinates: {coordinates}\r\n"
                , false, raidMessage.Embeds[0]);
                await raidInfoMessage.PinAsync();

                if (!_db.Lobbies.Contains(lobby))
                {
                    _db.Lobbies.Add(lobby);
                }

                await message.RespondAsync($"Raid lobby {lobbyName} was created successfully.");

                //TODO: Listen to commands in active raid lobby channels for .checkin and .ontheway
            }
        }
    }
}