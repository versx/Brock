namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Notifications,
        "Unsubscribe from a one or more or even all subscribed Pokemon notifications.",
        "\tExample: `.pokemenot 149`\r\n" +
        "\tExample: `.pokemenot 3,6,9,147,148,149`\r\n" +
        "\tExample: `.pokemenot` (Removes all subscribed Pokemon notifications.)",
        "pokemenot"
    )]
    public class PokeMeNotCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public PokeMeNotCommand(DiscordClient client, IDatabase db)
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
                await message.RespondAsync($"{message.Author.Mention} is not subscribed to any Pokemon notifications.");
                return;
            }

            if (!(command.HasArgs && command.Args.Count == 1))
            {
                if (!_db.RemoveAllPokemon(author))
                {
                    await message.RespondAsync($"Failed to remove all Pokemon notifications for {message.Author.Mention}.");
                    return;
                }

                await message.RespondAsync($"{message.Author.Mention} has unsubscribed from **all** Pokemon notifications.");
                return;
            }

            var notSubscribed = new List<string>();
            var unsubscribed = new List<string>();

            var cmd = command.Args[0];
            foreach (var arg in cmd.Split(','))
            {
                var index = Convert.ToUInt32(arg);
                if (!_db.Pokemon.ContainsKey(index.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, pokedex number {index} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = _db.Pokemon[index.ToString()];
                var unsubscribePokemon = _db[author].Pokemon.Find(x => x.PokemonId == index);
                if (unsubscribePokemon != null)
                {
                    if (_db[author].Pokemon.Remove(unsubscribePokemon))
                    {
                        //msg += $"**{pokemon.Name}**";
                        unsubscribed.Add(pokemon.Name);
                    }
                }
                else
                {
                    notSubscribed.Add(pokemon.Name);
                }
            }

            await message.RespondAsync
            (
                (unsubscribed.Count > 0
                    ? $"{message.Author.Mention} has unsubscribed from **{string.Join("**, **", unsubscribed)}** notifications."
                    : string.Empty) +
                (notSubscribed.Count > 0 
                    ? $" {message.Author.Mention} is not subscribed to {string.Join(", ", notSubscribed)} notifications." 
                    : string.Empty)
            );
        }
    }
}