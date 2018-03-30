namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;

    using BrockBot.Data.Models;

    public static class CalcIV
    {
        public static List<PokemonIV> CalculateRaidIVs(uint pokeId, PokemonBaseStats baseStats, int cp)
        {
            var list = new List<PokemonIV>();
            var cpm = 0.59740001;
            var lvl = 20;
            var perfectCP = Convert.ToInt32(((baseStats.Attack + 15.0) * Math.Pow(baseStats.Defense + 15.0, 0.5) * Math.Pow(baseStats.Stamina + 15.0, 0.5) * Math.Pow(cpm, 2)) / 10.0);

            //if (weather)
            //{
            //    cpm = 0.667934;
            //    lvl = 25;
            //}

            if (!(cp <= perfectCP))
            {
                //weather = true;
                cpm = 0.667934;
                lvl = 25;
            }

            for (int sta = 10; sta <= 15; sta++)
            {
                for (int atk = 10; atk <= 15; atk++)
                {
                    for (int def = 10; def <= 15; def++)
                    {
                        var attack = baseStats.Attack + atk;
                        var defense = baseStats.Defense + def;
                        var stamina = baseStats.Stamina + sta;
                        var currCP = Math.Floor((attack * Math.Sqrt(defense) * Math.Sqrt(stamina) * cpm * cpm) / 10);

                        if (Convert.ToInt32(currCP) == cp)
                        {
                            var hp = Convert.ToInt32(Math.Floor(cpm * (baseStats.Stamina + sta)));
                            var iv = Math.Round(Convert.ToDouble((sta + atk + def) * 100.0 / 45), 2);

                            list.Add(new PokemonIV
                            {
                                PokemonId = pokeId,
                                Attack = atk,
                                Defense = def,
                                Stamina = sta,
                                CP = cp,
                                HP = hp,
                                IV = iv,
                                Level = lvl
                            });
                        }
                    }
                }
            }

            list.Sort((x, y) => y.IV.CompareTo(x.IV));
            return list;
        }

        public static List<PokemonIV> CalculateIVs(uint pokeId, PokemonBaseStats baseStats, int cp, int health, bool hatched, Dictionary<string, double> ECpM)
        {
            var list = new List<PokemonIV>();
            int minAtk = 0, minDef = 0, minSta = 0;
            int maxAtk = 16, maxDef = 16, maxSta = 16;
            if (hatched)
            {
                minAtk = 10;
                minDef = 10;
                minSta = 10;
            }

            foreach (var cpm in ECpM)
            {
                if (hatched && cpm.Key != "20") continue;

                var lvl = cpm.Key;
                for (int sta = minSta; sta < maxSta; sta++)
                {
                    var hp = Convert.ToInt32(Math.Floor(ECpM[lvl] * (baseStats.Stamina + sta)));
                    hp = hp < 10 ? 10 : hp;
                    if (hp == health || health == 0)
                    {
                        for (int atk = minAtk; atk < maxAtk; atk++)
                        {
                            for (int def = minDef; def < maxDef; def++)
                            {
                                var currCP = Math.Floor((baseStats.Attack + atk) * Math.Pow(baseStats.Defense + def, 0.5) * Math.Pow(baseStats.Stamina + sta, 0.5) * Math.Pow(ECpM[lvl], 2) / 10);
                                currCP = currCP < 10 ? 10 : currCP;

                                if (Convert.ToInt32(currCP) == cp)
                                {
                                    list.Add(new PokemonIV
                                    {
                                        PokemonId = pokeId,
                                        Attack = atk,
                                        Defense = def,
                                        Stamina = sta,
                                        CP = cp,
                                        HP = hp,
                                        IV = Math.Round(Convert.ToDouble((sta + atk + def) * 100.0 / 45), 2),
                                        Level = Convert.ToDouble(lvl)
                                    });
                                }
                            }
                        }
                    }
                }
            }

            list.Sort((x, y) => y.IV.CompareTo(x.IV));
            return list;
        }
    }

    public class PokemonIV
    {
        public uint PokemonId { get; set; }

        public int CP { get; set; }

        public int HP { get; set; }

        public double Level { get; set; }

        public double IV { get; set; }

        public int Attack { get; set; }

        public int Defense { get; set; }

        public int Stamina { get; set; }
    }
}