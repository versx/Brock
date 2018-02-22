namespace BrockBot.Net
{
    using System;

    public sealed class RaidData
    {
        public int PokemonId { get; }

        public string Level { get; }

        public string CP { get; }

        public string FastMove { get; }

        public string ChargeMove { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public DateTime StartTime { get; }

        public DateTime EndTime { get; }

        public RaidData(int pokemonId, string level, string cp, string move1, string move2, double lat, double lng, DateTime startTime, DateTime endTime)
        {
            PokemonId = pokemonId;
            Level = level;
            CP = cp;
            FastMove = move1;
            ChargeMove = move2;
            Latitude = lat;
            Longitude = lng;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}