namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.Administrative,
        "Leaves the specified guild.",
        "\tExample: .leave 32098402384324",
        "leave"
    )]
    public class LeavelGuildCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => true;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public LeavelGuildCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var guildId = command.Args[0];
            if (ulong.TryParse(guildId, out ulong result))
            {
                var guild = await Client.GetGuildAsync(result);
                if (guild == null)
                {
                    await message.RespondAsync($"{guildId} is not a valid guild id.");
                    return;
                }

                await guild.LeaveAsync();
                await message.RespondAsync($"{FilterBot.BotName} successfully left guild {guild.Name} ({guild.Id}).");
                return;
            }

            foreach (var guild in Client.Guilds)
            {
                if (string.Compare(guild.Value.Name, guildId, true) == 0)
                {
                    await guild.Value.LeaveAsync();
                    await message.RespondAsync($"{FilterBot.BotName} successfully left guild {guild.Value.Name} ({guild.Value.Id}).");
                }
            }
        }
    }
}
