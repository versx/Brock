namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Services;

    [Command(
        Categories.General,
        "Retrieves weather conditions information for the specified city.",
        "\tExample: `.weather Upland`",
        "weather"
    )]
    public class GetWeatherCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IWeatherService _weatherSvc;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public GetWeatherCommand(DiscordClient client, Config config, IWeatherService weatherSvc)
        {
            _client = client;
            _config = config;
            _weatherSvc = weatherSvc;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs)
            {
                await message.RespondAsync($"{message.Author.Mention} please specify a city e.g. `{_config.CommandsPrefix}{command.Name} Upland`");
                return;
            }

            var city = command.Args[0];
            var weather = _weatherSvc.GetWeatherCondition(city);
            if (weather == null)
            {
                await message.RespondAsync($"{message.Author.Mention} failed to retrieve the weather conditions for {city}.");
                return;
            }

            await message.RespondAsync($"{message.Author.Mention} {city} {weather.WeatherText}");
        }
    }
}