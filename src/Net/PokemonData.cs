namespace BrockBot.Net
{
    using System;

    public sealed class PokemonData
    {
        public int PokemonId { get; }

        public string CP { get; }

        public string IV { get; }

        public string Stamina { get; }

        public string Attack { get; }

        public string Defense { get; }

        public PokemonGender Gender { get; }

        public string PlayerLevel { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public string FastMove { get; }

        public string ChargeMove { get; }

        public string Height { get; }

        public string Weight { get; }

        public DateTime DespawnTime { get; }

        public TimeSpan SecondsLeft { get; }

        public PokemonData(int pokemonId, string cp, string iv, string stamina, string attack, string defense, PokemonGender gender, string level, double lat, double lng, string move1, string move2, string height, string weight, DateTime despawn, TimeSpan secondsLeft)
        {
            PokemonId = pokemonId;
            CP = cp;
            IV = iv;
            Stamina = stamina;
            Attack = attack;
            Defense = defense;
            Gender = gender;
            PlayerLevel = level;
            Latitude = lat;
            Longitude = lng;
            FastMove = move1;
            ChargeMove = move2;
            Height = height;
            Weight = weight;
            DespawnTime = despawn;
            SecondsLeft = secondsLeft;
        }
    }
}