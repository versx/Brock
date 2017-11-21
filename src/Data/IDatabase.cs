namespace BrockBot.Data
{
    using System;
    using System.Collections.Generic;

    using BrockBot.Data.Models;

    public interface IDatabase
    {
        Server this[ulong guildId] { get; }

        Dictionary<string, PokemonInfo> Pokemon { get; }
    }
}