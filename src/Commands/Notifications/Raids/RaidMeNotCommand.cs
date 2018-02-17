namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;

    [Command(
        Categories.Notifications,
        "Unsubscribe from a one or more or even all subscribed Raid notifications.",
        "\tExample: `.raidmenot Absol`\r\n" +
        "\tExample: `.raidmenot Tyranitar,Snorlax`\r\n" +
        "\tExample: `.raidmenot all` (Removes all subscribed Raid notifications.)\r\n" +
        "\tExample: `.raidmenot all yes | y` (Skips the confirmation part of unsubscribing from all raid bosses.)",
        "raidmenot"
    )]
    public class RaidMeNotCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public RaidMeNotCommand(DiscordClient client, Config config, IDatabase db)
        {
            _client = client;
            _config = config;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;

            if (!_db.Exists(author))
            {
                await message.RespondAsync($"{message.Author.Mention} is not subscribed to any raid notifications.");
                return;
            }

            var notSubscribed = new List<string>();
            var unsubscribed = new List<string>();

            var cmd = command.Args[0];

            if (string.Compare(cmd, "all", true) == 0)
            {
                if (command.Args.Count != 2)
                {
                    await message.RespondAsync($"{message.Author.Mention} are you sure you want to remove **all** {_db[author].Pokemon.Count.ToString("N0")} of your raid boss subscriptions? If so, please reply back with `{_config.CommandsPrefix}{command.Name} all yes` to confirm.");
                    return;
                }

                var confirm = command.Args[1];
                if (!(confirm == "yes" || confirm == "y")) return;

                if (!_db.RemoveAllRaids(author))
                {
                    await message.RespondAsync($"Failed to remove all raid boss subscriptions for {message.Author.Mention}.");
                    return;
                }

                await message.RespondAsync($"{message.Author.Mention} has unsubscribed from **all** raid notifications!");
                return;
            }

            foreach (var arg in cmd.Split(','))
            {
                var pokeId = Helpers.PokemonIdFromName(_db, arg);
                if (pokeId == 0)
                {
                    await message.RespondAsync($"{message.Author.Mention}, failed to find raid Pokemon {arg}.");
                    return;
                }

                var pokemon = _db.Pokemon[pokeId.ToString()];
                var unsubscribePokemon = _db[author].Raids.Find(x => x.PokemonId == pokeId);
                if (unsubscribePokemon != null)
                {
                    if (_db[author].Raids.Remove(unsubscribePokemon))
                    {
                        unsubscribed.Add(pokemon.Name);
                        continue;
                    }
                }

                notSubscribed.Add(pokemon.Name);
            }

            await message.RespondAsync
            (
                (unsubscribed.Count > 0
                    ? $"{message.Author.Mention} has unsubscribed from **{string.Join("**, **", unsubscribed)}** raid notifications."
                    : string.Empty) +
                (notSubscribed.Count > 0
                    ? $" {message.Author.Mention} is not subscribed to {string.Join(",", notSubscribed)} raid notifications."
                    : string.Empty)
            );
        }
    }
}