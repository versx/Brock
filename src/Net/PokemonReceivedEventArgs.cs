namespace BrockBot.Net
{
    using System;

    public class PokemonReceivedEventArgs : EventArgs
    {
        public PokemonData Pokemon { get; }

        public PokemonReceivedEventArgs(PokemonData pokemon)
        {
            Pokemon = pokemon;
        }
    }
}