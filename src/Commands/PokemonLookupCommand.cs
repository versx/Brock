namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    public class PokemonLookupCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public PokemonLookupCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var pokeId = Convert.ToInt32(command.Args[0]);
            var pokemon = _db.Pokemon.Find(x => x.Id == pokeId);
            if (pokemon == null)
            {
                await message.RespondAsync($"Failed to lookup Pokemon with id {pokeId}.");
                return;
            }

            await message.RespondAsync
            (
                $"{pokemon.Name} (ID: {pokeId}, Gen: {pokemon.Stats.Generation}{(pokemon.Stats.Legendary ? " Legendary" : "")})\r\n" +
                $"Stamina: {pokemon.Stats.Stamina}, Attack: {pokemon.Stats.Attack}, Defense: {pokemon.Stats.Defense}\r\n" +
                $"Type: " + (string.IsNullOrEmpty(pokemon.Stats.Type2) ? pokemon.Stats.Type1 : $"{pokemon.Stats.Type1}/{pokemon.Stats.Type2}")
            );
        }
    }
}