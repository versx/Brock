namespace BrockBot.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using BrockBot.Data.Models;
    using BrockBot.Services;

    public interface IDatabase
    {
        #region Properties

        List<RaidLobby> Lobbies { get; }

        List<Subscription<Pokemon>> Subscriptions { get; }

        Dictionary<string, PokemonInfo> Pokemon { get; }

        Dictionary<string, Moveset> Movesets { get; }

        ConcurrentDictionary<ulong, List<Reminder>> Reminders { get; }

        Subscription<Pokemon> this[ulong userId] { get; }

        #endregion

        #region Methods

        bool Exists(ulong userId);

        bool RemoveAllPokemon(ulong userId);

        bool RemoveAllRaids(ulong userId);

        void Save();

        #endregion
    }
}