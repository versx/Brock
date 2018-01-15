namespace BrockBot.Data
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using BrockBot.Data.Models;
    using BrockBot.Services;

    public interface IDatabase
    {
        List<RaidLobby> Lobbies { get; }

        List<Subscription<Pokemon>> Subscriptions { get; }

        Dictionary<string, PokemonInfo> Pokemon { get; }

        ConcurrentDictionary<ulong, List<Reminder>> Reminders { get; }

        Subscription<Pokemon> this[ulong userId] { get; }


        bool SubscriptionExists(ulong userId);

        bool RemoveAllPokemon(ulong userId);

        bool RemoveAllRaids(ulong userId);

        void Save();
    }
}