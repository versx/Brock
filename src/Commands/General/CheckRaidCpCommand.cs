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
        "Checks a raid bosses IV.",
        "\tExample: `.raidiv <pokemon_name_or_id> <raid_cp>`\r\n" +
        "\tExample: `.raidiv lugia 2047`\r\n" +
        "\tExample: `.raidiv tyranitar 2621",
        "raidiv"
    )]
    public class CheckRaidCpCommand : ICustomCommand
    {
        #region Variables

        private readonly DiscordClient _client;
        private readonly IDatabase _db;
        private readonly IEventLogger _logger;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public CheckRaidCpCommand(DiscordClient client, IDatabase db, IEventLogger logger)
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

            if (!_db.Pokemon.ContainsKey(pokeId.ToString()))
            {
                await message.RespondAsync($"{message.Author.Mention} {pokeId} is not a valid Pokemon id.");
                return;
            }

            var pokemon = _db.Pokemon[pokeId.ToString()];
            var eb = new DiscordEmbedBuilder
            {
                Title = $"Raid Boss IV: {pokemon.Name}",
                ThumbnailUrl = string.Format(Strings.PokemonImage, pokeId, 0),
                Color = DiscordColor.Purple,
                Footer = new DiscordEmbedBuilder.EmbedFooter { Text = $"versx | {DateTime.Now}" }
            };

            var possibleIVs = CalcIV.CalculateRaidIVs(pokeId, pokemon.BaseStats, cp);
            var boosted = possibleIVs.Count > 0 && Convert.ToInt32(possibleIVs[0].Level) == 25;
            var msg = string.Empty;
            msg += $"CP: {cp}\r\n";

            if (possibleIVs.Count == 0)
            {
                msg += "\r\nNo combinations found...";
            }
            else
            {
                msg += $":white_sun_rain_cloud: Boosted: {(boosted ? "Yes" : "No")}\r\n\r\n";
            }

            foreach (var possibleIV in possibleIVs)
            {
                msg += $"**{possibleIV.IV}%** ({possibleIV.Attack}/{possibleIV.Defense}/{possibleIV.Stamina}) L{possibleIV.Level} {possibleIV.HP} HP\r\n";

                //eb.Color = DiscordHelpers.BuildColor(possibleIV.IV.ToString());
            }
            eb.Description = msg;
            var embed = eb.Build();
            if (embed == null) return;

            await message.RespondAsync($"{message.Author.Mention}", false, embed);
        }
    }
}