namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Services;

    [Command(
        Categories.Administrative,
        "Starts the specified feed instance.",
        "\tExample: `.stop Upland`\r\n" +
        "\tExample: `.stop Upland,Ontario`\r\n" +
        "\tExample: `.stop All`",
        "stop"
    )]
    public class StopFeedCommand : ICustomCommand
    {
        private const string All = "all";

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public StopFeedCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

            var city = command.Args[0];

            var feeds = string.Compare(city, All, true) == 0 ? _config.CityRoles : new List<string>(city.Split(','));
            await StopFeeds(message, feeds);
        }

        private async Task StopFeeds(DiscordMessage message, List<string> feeds)
        {
            var msg = string.Empty;
            var started = new List<string>();
            var failed = new List<string>();
            foreach (var cityName in feeds)
            {
                if (TaskManager.StopTask("RM " + cityName))
                {
                    started.Add(cityName);
                }
                else
                {
                    failed.Add(cityName);
                }
            }

            await message.RespondAsync
            (
                (started.Count > 0
                    ? $"{message.Author.Mention} stopped feed(s) **{string.Join("**, **", started)}**."
                    : string.Empty) +
                (failed.Count > 0
                    ? $"\r\n{message.Author.Mention} failed to stop feed(s) **{string.Join("**, **", failed)}**."
                    : string.Empty)
            );
        }
    }
}