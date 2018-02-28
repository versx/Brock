namespace BrockBot.Net
{
    using System;

    public sealed class PokemonData
    {
        #region Properties

        public int Id { get; }

        public string CP { get; }

        public string IV { get; }

        public string Stamina { get; }

        public string Attack { get; }

        public string Defense { get; }

        public PokemonGender Gender { get; }

        public string Level { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public string FastMove { get; }

        public string ChargeMove { get; }

        public string Height { get; }

        public string Weight { get; }

        public DateTime DespawnTime { get; }

        public TimeSpan SecondsLeft { get; }

        public string FormId { get; }

        #endregion

        #region Constructor

        public PokemonData(int id, string cp, string iv, string sta, string atk, string def, PokemonGender gender, string lvl, double lat, double lng, string move1, string move2, string height, string weight, DateTime despawn, TimeSpan secondsLeft, string formId = null)
        {
            Id = id;
            CP = cp;
            IV = iv;
            Stamina = sta;
            Attack = atk;
            Defense = def;
            Gender = gender;
            Level = lvl;
            Latitude = lat;
            Longitude = lng;
            FastMove = move1;
            ChargeMove = move2;
            Height = height;
            Weight = weight;
            DespawnTime = despawn;
            SecondsLeft = secondsLeft;
            FormId = formId;
        }

        #endregion
    }
}