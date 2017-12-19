namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.RaidLobby,
        "Cancels your .otw or .here command.",
        "\tExample: .cancel ttar_test",
        "cancel"
    )]
    public class RaidLobbyCancelCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public RaidLobbyCancelCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            await message.IsDirectMessageSupported();

            var lobbyName = command.Args[0];
            if (string.IsNullOrEmpty(lobbyName))
            {
                await message.RespondAsync("You must enter a lobby name in order to send the on the way command.");
                return;
            }

            var server = Db[message.Channel.GuildId];
            if (server == null) return;

            var lobby = server.Lobbies.Find(x => string.Compare(x.LobbyName, lobbyName, true) == 0);
            if (lobby == null)
            {
                await message.RespondAsync($"Lobby {lobbyName} does not exist.");
                return;
            }

            var lobbyChannel = await Client.GetChannel(lobby.ChannelId);
            if (lobbyChannel == null)
            {
                await message.RespondAsync("Unrecognized lobby name.");
                return;
            }

            if (lobby.UserCheckInList.ContainsKey(message.Author.Id))
            {
                if (lobby.UserCheckInList.Remove(lobby.UserCheckInList[message.Author.Id]))
                {
                    await message.RespondAsync($"You have cancelled your interest in raid lobby {lobbyName}.");
                    await lobbyChannel.SendMessageAsync($"{message.Author.Mention} has cancelled their interest in raid lobby **{lobbyChannel.Name}**.");
                }
            }
            else
            {
                await message.RespondAsync($"Could not cancel raid lobby interest for {lobbyName}.");
            }
        }
    }
}