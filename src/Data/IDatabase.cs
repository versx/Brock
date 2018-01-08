namespace BrockBot.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using BrockBot.Data.Models;
    using BrockBot.Services;

    public interface IDatabase
    {
        Server this[ulong guildId] { get; }

        Dictionary<string, PokemonInfo> Pokemon { get; }

        ConcurrentDictionary<ulong, List<Reminder>> Reminders { get; }

        void Save();
    }
}