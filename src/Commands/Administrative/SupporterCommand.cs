﻿namespace BrockBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Utilities;

    [Command(
        Categories.Administrative,
        "Adds the specified user to the supporter list.",
        "\tExample: `.supporter @mention 6-12-2018 31`\r\n" +
        "\tExample: `.supporter 398423424 6-12-2018 31`",
        "supporter"
    )]
    public class SupporterCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Admin;

        public SupporterCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 3) return;

            var mention = command.Args[0];
            var date = command.Args[1];
            var days = command.Args[2];

            var userId = DiscordHelpers.ConvertMentionToUserId(mention);
            if (userId == 0)
            {
                await message.RespondAsync($"{message.Author.Mention}, I failed to lookup discord user {mention}.");
                return;
            }

            if (!DateTime.TryParse(date, out DateTime dateDonated))
            {
                await message.RespondAsync($"{message.Author.Mention} {date} is not a valid value for date.");
                return;
            }

            if (!int.TryParse(days, out int daysAvailable))
            {
                await message.RespondAsync($"{message.Author.Mention} {days} is not a valid value for days.");
                return;
            }

            if (!_config.Supporters.ContainsKey(userId))
            {
                _config.Supporters.Add(userId, new Data.Models.Donator { UserId = userId, Email = mention, DateDonated = dateDonated, DaysAvailable = daysAvailable });
            }
            else
            {
                var donator = _config.Supporters[userId];
                donator.DateDonated = dateDonated;
                donator.DaysAvailable = daysAvailable;
            }

            _config.Save();

            await message.RespondAsync($"{message.Author.Mention} {userId} has been added to the supporters list with {daysAvailable} days available.");
        }
    }
}