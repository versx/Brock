namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;

    [Command("poke")]
    public class PokemonLookupCommand : ICustomCommand
    {
        #region Properties

        public bool AdminCommand => false;

        public DiscordClient Client { get; }

        public IDatabase Db { get; }

        #endregion

        #region Constructor

        public PokemonLookupCommand(DiscordClient client, IDatabase db)
        {
            Client = client;
            Db = db;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var pokeId = Convert.ToInt32(command.Args[0]);
            if (!Db.Pokemon.ContainsKey(pokeId.ToString()))
            {
                await message.RespondAsync($"Failed to lookup Pokemon with id {pokeId}.");
            }

            var pokemon = Db.Pokemon[pokeId.ToString()];

            var types = pokemon.Types.Count > 1 ? pokemon.Types[0].Type + "/" + pokemon.Types[1].Type : pokemon.Types[0].Type;

            await message.RespondAsync
            (
                $"{pokemon.Name} (ID: {pokeId}, Gen: {pokemon.BaseStats.Generation}{(pokemon.BaseStats.Legendary ? " Legendary" : "")})\r\n" +
                $"Stamina: {pokemon.BaseStats.Stamina}, Attack: {pokemon.BaseStats.Attack}, Defense: {pokemon.BaseStats.Defense}\r\n" +
                $"Type: {types}"
            );
        }
    }
}