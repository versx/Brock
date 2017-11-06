﻿namespace PokeFilterBot.Commands
{
    using System;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using PokeFilterBot.Data;
    using PokeFilterBot.Extensions;

    public class RemoveCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public RemoveCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.HasArgs && command.Args.Count == 1)
            {
                var author = message.Author.Id;
                foreach (var chlName in command.Args[0].Split(','))
                {
                    var channelName = chlName;
                    if (channelName[0] == '#') channelName = channelName.Remove(0, 1);

                    var channel = _client.GetChannelByName(channelName);
                    if (channel == null)
                    {
                        await message.RespondAsync($"Channel name {channelName} is not a valid channel.");
                        continue;
                    }

                    if (!_db.Subscriptions.ContainsKey(author))
                    {
                        await message.RespondAsync($"You are not currently subscribed to any Pokemon notifications from any channels.");
                    }
                    else
                    {
                        //User has already subscribed before, check if their new requested sub already exists.
                        if (_db.Subscriptions[author].Channels.Contains(channel.Id))
                        {
                            if (_db.Subscriptions[author].Channels.Remove(channel.Id))
                            {
                                await message.RespondAsync($"You have successfully unsubscribed from #{channel.Name} Pokemon notifications.");
                            }
                        }
                        else
                        {
                            await message.RespondAsync($"You are not currently subscribed to any #{channel.Name} Pokemon notifications.");
                        }
                    }
                }
            }
        }
    }
}