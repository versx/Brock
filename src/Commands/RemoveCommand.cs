//namespace BrockBot.Commands
//{
//    using System;
//    using System.Threading.Tasks;

//    using DSharpPlus;
//    using DSharpPlus.Entities;

//    using BrockBot.Data;
//    using BrockBot.Extensions;

//    [Command(
//        Categories.Notifications,
//        "Removes the selected channels from being notified of Pokemon.",
//        "\tExample: `.remove channel1,channel2`\r\n" +
//        "\tExample: `.remove single_channel1`",
//        "remove"
//    )]
//    public class RemoveCommand : ICustomCommand
//    {
//        #region Properties

//        public bool AdminCommand => false;

//        public DiscordClient Client { get; }

//        public IDatabase Db { get; }

//        #endregion

//        #region Constructor

//        public RemoveCommand(DiscordClient client, IDatabase db)
//        {
//            Client = client;
//            Db = db;
//        }

//        #endregion

//        public async Task Execute(DiscordMessage message, Command command)
//        {
//            if (!command.HasArgs) return;
//            if (command.Args.Count != 1) return;

//            await message.IsDirectMessageSupported();

//            var server = Db[message.Channel.GuildId];

//            var author = message.Author.Id;
//            foreach (var chlName in command.Args[0].Split(','))
//            {
//                var channelName = chlName;
//                if (channelName[0] == '#') channelName = channelName.Remove(0, 1);

//                var channel = Client.GetChannelByName(channelName);
//                if (channel == null)
//                {
//                    await message.RespondAsync($"Channel name {channelName} is not a valid channel.");
//                    continue;
//                }

//                if (!server.SubscriptionExists(author))
//                {
//                    await message.RespondAsync($"You are not currently subscribed to any Pokemon notifications from any channels.");
//                }
//                else
//                {
//                    //User has already subscribed before, check if their new requested sub already exists.
//                    if (server[author].ChannelIds.Contains(channel.Id))
//                    {
//                        if (server[author].ChannelIds.Remove(channel.Id))
//                        {
//                            await message.RespondAsync($"You have successfully unsubscribed from #{channel.Name} Pokemon notifications.");
//                        }
//                    }
//                    else
//                    {
//                        await message.RespondAsync($"You are not currently subscribed to any #{channel.Name} Pokemon notifications.");
//                    }
//                }
//            }
//        }
//    }
//}