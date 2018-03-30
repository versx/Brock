namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;
    using BrockBot.Services;

    [Command(
        Categories.General,
        "Checks a wild or hatched Pokemon's IV combinations.",
        "\tExample: `.checkiv <pokemon_name_or_id> <cp>`\r\n" +
        "\tExample: `.checkiv <pokemon_name_or_id> <cp> <hp> <hatched>`\r\n" +
        "\tExample: `.checkiv larvitar 517`\r\n" +
        "\tExample: `.checkiv dratini 491 yes",
        "checkiv"
    )]
    public class CheckCpCommand : ICustomCommand
    {
        private const int MaxCombinationsShown = 60;

        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public CheckCpCommand(DiscordClient client, IDatabase db, IEventLogger logger)
        {
            _client = client;
            _db = db;
            _logger = logger;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            var cmd = command.Args[0];
            var cpArg = command.Args[1];
            var hpArg = command.Args.Count >= 3 ? command.Args[2] : "0";
            var hatched = command.Args.Count == 4 && command.Args[3].StartsWith("y", StringComparison.Ordinal);

            if (!uint.TryParse(cmd, out uint pokeId))
            {
                pokeId = _db.PokemonIdFromName(cmd);

                if (pokeId == 0)
                {
                    await message.RespondAsync($"{message.Author.Mention} failed to lookup Pokemon by name and pokedex id {cmd}.");
                    return;
                }
            }

            if (!int.TryParse(cpArg, out int cp))
            {
                await message.RespondAsync($"{message.Author.Mention} {cpArg} is not a valid CP value.");
                return;
            }

            if (!int.TryParse(hpArg, out int hp))
            {
                await message.RespondAsync($"{message.Author.Mention} {hpArg} is not a valid HP value.");
                return;
            }

            if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
            {
                await message.RespondAsync($"{message.Author.Mention} {pokeId} is not a valid Pokemon id.");
                return;
            }

            var pokemon = _db.Pokemon[pokeId.ToString()];
            var eb = new DiscordEmbedBuilder
            {
                Title = $"Pokemon IV: {pokemon.Name}",
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokeId, 0),
                Color = DiscordColor.Green,
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"versx | {DateTime.Now}" }
            };

            var possibleIVs = CalcIV.CalculateIVs(pokeId, pokemon.BaseStats, cp, hp, hatched, _db.CpMultipliers);
            var msg = string.Empty;
            msg += $"CP: {cp}\r\n";
            msg += $":egg: Hatch: {(hatched ? "Yes" : "No")}\r\n\r\n";
            var exceedsLimits = false;
            if (possibleIVs.Count > MaxCombinationsShown)
            {
                possibleIVs = possibleIVs.GetRange(0, Math.Min(MaxCombinationsShown, possibleIVs.Count));
                exceedsLimits = true;
            }
            else if (possibleIVs.Count == 0)
            {
                msg += "No combinations found...";
            }
            foreach (var possibleIV in possibleIVs)
            {
                msg += $"**{possibleIV.IV}%** ({possibleIV.Attack}/{possibleIV.Defense}/{possibleIV.Stamina}) L{possibleIV.Level} {possibleIV.HP} HP\r\n";

                //eb.Color = DiscordHelpers.BuildColor(possibleIV.IV.ToString());
            }
            if (exceedsLimits)
            {
                msg += "\r\nMore combinations not shown...";
            }
            eb.Description = msg;
            var embed = eb.Build();
            if (embed == null) return;

            await message.RespondAsync($"{message.Author.Mention}", false, embed);
        }
    }
}