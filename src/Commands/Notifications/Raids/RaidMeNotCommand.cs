namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command(
        Categories.Notifications,
        "Unsubscribe from a one or more or even all subscribed Raid notifications.",
        "\tExample: `.raidmenot Absol`\r\n" +
        "\tExample: `.raidmenot Tyranitar,Snorlax`\r\n" +
        "\tExample: `.raidmenot` (Removes all subscribed Raid notifications.)",
        "raidmenot"
    )]
    public class RaidMeNotCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public RaidMeNotCommand(DiscordClient client, IDatabase db)
        {
            _client = client;
            _db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            //await message.IsDirectMessageSupported();

            var author = message.Author.Id;

            if (!_db.SubscriptionExists(author))
            {
                await message.RespondAsync($"{message.Author.Mention} is not subscribed to any raid notifications.");
                return;
            }

            if (!command.HasArgs)
            {
                _db.RemoveAllRaids(author);
                await message.RespondAsync($"{message.Author.Mention} has unsubscribed from **all** raid notifications!");
                return;
            }

            if (command.Args.Count != 1) return;

            var notSubscribed = new List<string>();
            var unsubscribed = new List<string>();

            var cmd = command.Args[0];
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