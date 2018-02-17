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
        #region Constants

        private const string WeatherIconUrl = "http://ver.sx/x/Brock/Weather/Icons/{0}-s.png";

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IWeatherService _weatherSvc;

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.User;

        #endregion

        #region Constructor

        public GetWeatherCommand(DiscordClient client, Config config, IWeatherService weatherSvc)
        {
            _client = client;
            _config = config;
            _weatherSvc = weatherSvc;
        }

        #endregion

        #region Public Methods

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs)
            {
                await message.RespondAsync($"{message.Author.Mention} please specify a city e.g. `{_config.CommandsPrefix}{command.Name} Upland`");
                return;
            }

            var city = command.Args[0];
            if (!_config.CityRoles.Exists(x => string.Compare(x, city, true) == 0))
            {
                await message.RespondAsync($"{message.Author.Mention} you may only check weather conditions of one of the following cities: **{string.Join("**, **", _config.CityRoles)}**.");
                return;
            }

            var weather = _weatherSvc.GetWeatherCondition(city);
            if (weather == null)
            {
                await message.RespondAsync($"{message.Author.Mention} failed to retrieve the weather conditions for {city}.");
                return;
            }

            var eb = new DiscordEmbedBuilder();
            eb.WithTitle($"{city} Weather Conditions");
            eb.AddField("Weather", weather.WeatherText, true);
            eb.AddField("Temperature", $"{weather.Temperature.Imperial.Value}°{weather.Temperature.Imperial.Unit}", true);
            eb.WithImageUrl(string.Format(WeatherIconUrl, weather.WeatherIcon.ToString("D2")));
            eb.WithUrl(weather.Link);

            var embed = eb.Build();
            if (embed == null) return;

            await message.RespondAsync(string.Empty, false, embed);
        }

        #endregion
    }
}