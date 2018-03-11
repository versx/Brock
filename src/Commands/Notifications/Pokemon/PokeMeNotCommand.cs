namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Data;
    using BrockBot.Extensions;

    [Command(
        Categories.Notifications,
        "Unsubscribe from a one or more or even all subscribed Pokemon notifications by pokedex number or name.",
        "\tExample: `.pokemenot 149`\r\n" +
        "\tExample: `.pokemenot pikachu`\r\n" +
        "\tExample: `.pokemenot 3,6,9,147,148,149`\r\n" +
        "\tExample: `.pokemenot bulbasuar,7,tyran`\r\n" +
        "\tExample: `.pokemenot all` (Removes all subscribed Pokemon notifications.)\r\n" +
        "\tExample: `.pokemenot all yes | y` (Skips the confirmation part of unsubscribing from all Pokemon.)",
        "pokemenot"
    )]
    public class PokeMeNotCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IDatabase _db;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public PokeMeNotCommand(DiscordClient client, Config config, IDatabase db)
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
                await message.RespondAsync($"{message.Author.Mention} is not subscribed to any Pokemon notifications.");
                return;
            }

            var notSubscribed = new List<string>();
            var unsubscribed = new List<string>();

            var cmd = command.Args[0];

            if (string.Compare(cmd, "all", true) == 0)
            {
                if (command.Args.Count != 2)
                {
                    await message.RespondAsync($"{message.Author.Mention} are you sure you want to remove **all** {_db[author].Pokemon.Count.ToString("N0")} of your Pokemon subscriptions? If so, please reply back with `{_config.CommandsPrefix}{command.Name} all yes` to confirm.");
                    return;
                }

                var confirm = command.Args[1];
                if (!(confirm == "yes" || confirm == "y")) return;

                if (!_db.RemoveAllPokemon(author))
                {
                    await message.RespondAsync($"Failed to remove all Pokemon subscriptions for {message.Author.Mention}.");
                    return;
                }

                await message.RespondAsync($"{message.Author.Mention} has unsubscribed from **all** Pokemon notifications.");
                return;
            }

            foreach (var arg in cmd.Split(','))
            {
                var pokeId = _db.PokemonIdFromName(arg);
                if (pokeId == 0)
                {
                    if (!uint.TryParse(arg, out pokeId))
                    {
                        await message.RespondAsync($"{message.Author.Mention}, failed to lookup Pokemon by name and pokedex id using {arg}.");
                        return;
                    }
                }

                //var index = Convert.ToUInt32(arg);
                if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
                {
                    await message.RespondAsync($"{message.Author.Mention}, pokedex number {pokeId} is not a valid Pokemon id.");
                    continue;
                }

                var pokemon = _db.Pokemon[pokeId.ToString()];
                var unsubscribePokemon = _db[author].Pokemon.Find(x => x.PokemonId == pokeId);
                if (unsubscribePokemon != null)
                {
                    if (_db[author].Pokemon.Remove(unsubscribePokemon))
                    {
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
                    ? $" {message.Author.Mention} is not subscribed to **{string.Join("**, **", notSubscribed)}** notifications." 
                    : string.Empty)
            );
        }
    }
}