namespace BrockBot.Extensions
{
    using System;
    using System.Collections.Generic;

    using BrockBot.Data;

    public class CpRange
    {
        public int Worst { get; }

        public int Best { get; }

        public CpRange(int worst, int best)
        {
            Worst = worst;
            Best = best;
        }
    }

    public static class PokemonExtensions
    {
        private static double[] cpMultipliers =
        {
            0.094, 0.16639787, 0.21573247, 0.25572005, 0.29024988,
            0.3210876, 0.34921268, 0.37523559, 0.39956728, 0.42250001,
            0.44310755, 0.46279839, 0.48168495, 0.49985844, 0.51739395,
            0.53435433, 0.55079269, 0.56675452, 0.58227891, 0.59740001,
            0.61215729, 0.62656713, 0.64065295, 0.65443563, 0.667934,
            0.68116492, 0.69414365, 0.70688421, 0.71939909, 0.7317,
            0.73776948, 0.74378943, 0.74976104, 0.75568551, 0.76156384,
            0.76739717, 0.7731865, 0.77893275, 0.78463697, 0.79030001
        };

        public static uint PokemonIdFromName(this IDatabase db, string name)
        {
            foreach (var p in db.Pokemon)
            {
                if (p.Value.Name.ToLower().Contains(name.ToLower()))
                {
                    return Convert.ToUInt32(p.Key);
                }
            }

            return 0;
        }

        public static CpRange GetPokemonCpRange(this IDatabase db, int pokeId, int level)
        {
            if (!db.Pokemon.ContainsKey(pokeId.ToString())) return null;

            var baseStats = db.Pokemon[pokeId.ToString()];
            var baseAtk = baseStats.BaseStats.Attack;
            var baseDef = baseStats.BaseStats.Defense;
            var baseSta = baseStats.BaseStats.Stamina;
            var cpMulti = db.CpMultipliers[level.ToString()];

            int minCp = Convert.ToInt32(((baseAtk + 10.0) * Math.Pow(baseDef + 10.0, 0.5)
                * Math.Pow(baseSta + 10.0, 0.5) * Math.Pow(cpMulti, 2)) / 10.0);
            int maxCp = Convert.ToInt32(((baseAtk + 15.0) * Math.Pow(baseDef + 15.0, 0.5)
                * Math.Pow(baseSta + 15.0, 0.5) * Math.Pow(cpMulti, 2)) / 10.0);

            return new CpRange(minCp, maxCp);
            //Console.WriteLine($"CP Range: {minCp}-{maxCp} at L{level}");
        }

        public static string GetPokemonForm(this int pokeId, string formId)
        {
            if (!int.TryParse(formId, out int form)) return null;

            switch (pokeId)
            {
                case 201: //Unown
                    switch (form)
                    {
                        case 27:
                            return "!";
                        case 28:
                            return "?";
                        default:
                            return form.NumberToAlphabet(true).ToString();
                    }
                case 351: //Castform
                    switch (form)
                    {
                        case 29: //Normal
                            break;
                        case 30: //Sunny
                            return "Sunny";
                        case 31: //Water
                            return "Rainy";
                        case 32: //Snow
                            return "Snowy";
                    }
                    break;
                case 386: //Deoxys
                    return "N/A";
            }

            return null;
        }

        public static string GetPokemonGenderIcon(this PokemonGender gender)
        {
            switch (gender)
            {
                case PokemonGender.Male:
                    return "♂";//\u2642
                case PokemonGender.Female:
                    return "♀";//\u2640
                default:
                    return "⚲";//?
            }
        }

        public static string GetSize(this IDatabase db, int id, float height, float weight)
        {
            if (!db.Pokemon.ContainsKey(id.ToString())) return string.Empty;

            var stats = db.Pokemon[id.ToString()];
            float weightRatio = weight / (float)stats.BaseStats.Weight;
            float heightRatio = height / (float)stats.BaseStats.Height;
            float size = heightRatio + weightRatio;

            if (size < 1.5) return "Tiny";
            if (size <= 1.75) return "Small";
            if (size < 2.25) return "Normal";
            if (size <= 2.5) return "Large";
            return "Big";
        }

        public static int MaxCpAtLevel(this IDatabase db, int id, int level)
        {
            var multiplier = cpMultipliers[level - 1];
            var atk = (BaseAtk(db, id) + 15) * multiplier;
            var def = (BaseDef(db, id) + 15) * multiplier;
            var sta = (BaseSta(db, id) + 15) * multiplier;
            return (int)Math.Max(10, Math.Floor(Math.Sqrt(atk * atk * def * sta) / 10));
        }

        public static int GetLevel(double cpModifier)
        {
            double unRoundedLevel;

            if (cpModifier < 0.734)
            {
                unRoundedLevel = (58.35178527 * cpModifier * cpModifier - 2.838007664 * cpModifier + 0.8539209906);
            }
            else
            {
                unRoundedLevel = 171.0112688 * cpModifier - 95.20425243;
            }

            return (int)Math.Round(unRoundedLevel);
        }

        public static int GetRaidBossCp(this IDatabase db, int bossId, int raidLevel)
        {
            int stamina = 600;

            switch (raidLevel)
            {
                case 1:
                    stamina = 600;
                    break;
                case 2:
                    stamina = 1800;
                    break;
                case 3:
                    stamina = 3000;
                    break;
                case 4:
                    stamina = 7500;
                    break;
                case 5:
                    stamina = 12500;
                    break;
            }
            return (int)Math.Floor(((BaseAtk(db, bossId) + 15) * Math.Sqrt(BaseDef(db, bossId) + 15) * Math.Sqrt(stamina)) / 10);
        }

        public static double BaseAtk(this IDatabase db, int id)
        {
            if (!db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = db.Pokemon[id.ToString()];

            return stats.BaseStats.Attack;
        }

        public static double BaseDef(this IDatabase db, int id)
        {
            if (!db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = db.Pokemon[id.ToString()];

            return stats.BaseStats.Defense;
        }

        public static double BaseSta(this IDatabase db, int id)
        {
            if (!db.Pokemon.ContainsKey(id.ToString())) return 0;

            var stats = db.Pokemon[id.ToString()];

            return stats.BaseStats.Stamina;
        }

        public static List<string> GetStrengths(string type)
        {
            var types = new string[0];
            switch (type.ToLower())
            {
                case "normal":
                    break;
                case "fighting":
                    types = new string[] { "Normal", "Rock", "Steel", "Ice", "Dark" };
                    break;
                case "flying":
                    types = new string[] { "Fighting", "Bug", "Grass" };
                    break;
                case "poison":
                    types = new string[] { "Grass", "Fairy" };
                    break;
                case "ground":
                    types = new string[] { "Poison", "Rock", "Steel", "Fire", "Electric" };
                    break;
                case "rock":
                    types = new string[] { "Flying", "Bug", "Fire", "Ice" };
                    break;
                case "bug":
                    types = new string[] { "Grass", "Psychic", "Dark" };
                    break;
                case "ghost":
                    types = new string[] { "Ghost", "Psychic" };
                    break;
                case "steel":
                    types = new string[] { "Rock", "Ice" };
                    break;
                case "fire":
                    types = new string[] { "Bug", "Steel", "Grass", "Ice" };
                    break;
                case "water":
                    types = new string[] { "Ground", "Rock", "Fire" };
                    break;
                case "grass":
                    types = new string[] { "Ground", "Rock", "Water" };
                    break;
                case "electric":
                    types = new string[] { "Flying", "Water" };
                    break;
                case "psychic":
                    types = new string[] { "Fighting", "Poison" };
                    break;
                case "ice":
                    types = new string[] { "Flying", "Ground", "Grass", "Dragon" };
                    break;
                case "dragon":
                    types = new string[] { "Dragon" };
                    break;
                case "dark":
                    types = new string[] { "Ghost", "Psychic" };
                    break;
                case "fairy":
                    types = new string[] { "Fighting", "Dragon", "Dark" };
                    break;
            }
            return new List<string>(types);
        }

        public static List<string> GetWeaknesses(string type)
        {
            var types = new string[0];
            switch (type.ToLower())
            {
                case "normal":
                    types = new string[] { "Fighting" };
                    break;
                case "fighting":
                    types = new string[] { "Flying", "Psychic", "Fairy" };
                    break;
                case "flying":
                    types = new string[] { "Rock", "Electric", "Ice" };
                    break;
                case "poison":
                    types = new string[] { "Ground", "Psychic" };
                    break;
                case "ground":
                    types = new string[] { "Water", "Grass", "Ice" };
                    break;
                case "rock":
                    types = new string[] { "Fighting", "Ground", "Steel", "Water", "Grass" };
                    break;
                case "bug":
                    types = new string[] { "Flying", "Rock", "Fire" };
                    break;
                case "ghost":
                    types = new string[] { "Ghost", "Dark" };
                    break;
                case "steel":
                    types = new string[] { "Fighting", "Ground", "Fire" };
                    break;
                case "fire":
                    types = new string[] { "Ground", "Rock", "Water" };
                    break;
                case "water":
                    types = new string[] { "Grass", "Electric" };
                    break;
                case "grass":
                    types = new string[] { "Flying", "Poison", "Bug", "Fire", "Ice" };
                    break;
                case "electric":
                    types = new string[] { "Ground" };
                    break;
                case "psychic":
                    types = new string[] { "Bug", "Ghost", "Dark" };
                    break;
                case "ice":
                    types = new string[] { "Fighting", "Rock", "Steel", "Fire" };
                    break;
                case "dragon":
                    types = new string[] { "Ice", "Dragon", "Fairy" };
                    break;
                case "dark":
                    types = new string[] { "Fighting", "Bug", "Fairy" };
                    break;
                case "fairy":
                    types = new string[] { "Poison", "Steel" };
                    break;
            }
            return new List<string>(types);
        }
    }
}