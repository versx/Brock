namespace BrockBot.Extensions
{
    using System;
    using System.Collections.Generic;

    public static class Extensions
    {
        public static bool IsValidRaidBoss(this uint pokeId, List<uint> raidBosses)
        {
            return raidBosses.Contains(pokeId);
        }
    }
}