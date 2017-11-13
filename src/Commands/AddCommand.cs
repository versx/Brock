namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Data;
    using BrockBot.Data.Models;
    using BrockBot.Extensions;

    /**Example Usage:
     * .add upland_rares,ontario_rares
     * .add upland_100iv
     */
    public class AddCommand : ICustomCommand
    {
        private readonly DiscordClient _client;
        private readonly Database _db;

        public bool AdminCommand => false;

        public AddCommand(DiscordClient client, Database db)
        {
            _client = client;
            _db = db;
        }

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (!command.HasArgs) return;
            if (command.Args.Count != 1) return;

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

                if (message.Channel == null) return;
                var server = _db[message.Channel.GuildId];
                if (server == null) return;

                if (!server.ContainsKey(author))
                {
                    server.Subscriptions.Add(new Subscription(author, new List<uint>(), new List<ulong> { channel.Id }));
                    await message.RespondAsync($"You have successfully subscribed to #{channel.Name} notifications!");
                }
                else
                {
                    //User has already subscribed before, check if their new requested sub already exists.
                    if (!server[author].ChannelIds.Contains(channel.Id))
                    {
                        server[author].ChannelIds.Add(channel.Id);
                        await message.RespondAsync($"You have successfully subscribed to #{channel.Name} notifications!");
                    }
                    else
                    {
                        await message.RespondAsync($"You are already subscribed to #{channel.Name} notifications.");
                    }
                }
            }
        }
    }
}