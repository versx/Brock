namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;

    [Command(Categories.Moderator,
        "Sets notifications for Community Day to only send event Pokemon with a specific minimum IV..",
        "\tExample: `.event 90 146,147,148`\r\n" +
        "\tExample: `.event true/false`",
        "event"
    )]
    public class PokemonEventCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly IEventLogger _logger;
        private readonly Config _config;

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Moderator;

        #endregion

        #region Constructor

        public PokemonEventCommand(DiscordClient client, IEventLogger logger, Config config)
        {
            _client = client;
            _logger = logger;
            _config = config;
        }

        #endregion

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;

            switch (command.Args.Count)
            {
                case 1:
                    //Enable/Disable
                    var eventCmd = command.Args[0];
                    if (!bool.TryParse(eventCmd, out bool enable))
                    {
                        await message.RespondAsync($"{message.Author.Mention} {eventCmd} is not valid.");
                        return;
                    }

                    _config.OnlySendEventPokemon = enable;
                    _config.Save();

                    await message.RespondAsync($"{message.Author.Mention} switched event settings 'OnlySendEventPokemon' to {enable}");
                    break;
                case 2:
                    //Set minimum IV and event Pokemon list
                    var eventIV = command.Args[0];
                    var mons = command.Args[1];

                    if (!int.TryParse(eventIV, out int minimumIV))
                    {
                        await message.RespondAsync($"{message.Author.Mention} {eventIV} is not valid.");
                        return;
                    }

                    var list = new List<string>(mons.Split(','));
                    var pokemon = list.ConvertAll(Convert.ToUInt32);

                    _config.EventPokemonMinimumIV = minimumIV;
                    _config.EventPokemon = pokemon;
                    _config.Save();

                    await message.RespondAsync($"{message.Author.Mention} switched event settings to minimum IV {minimumIV}% for Pokemon {mons}.");
                    break;
            }
        }
    }
}