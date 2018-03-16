namespace BrockBot.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using DSharpPlus;
    using DSharpPlus.Entities;

    using BrockBot.Configuration;
    using BrockBot.Diagnostics;
    using BrockBot.Extensions;

    [Command(
        Categories.Voting,
        "Creates a poll for users to vote on based on emoji reactions.",
        "\tExample: `.poll \"This is a voting poll question?\" \"Answer1,Answer2,Answer3\"`\r\n" +
        "\tExample: `.poll \"This is another example poll?\" \"AnswerA,AnswerB,AnswerC\" @everyone`\r\n",
        "poll"
    )]
    public class CreateVoteCommand : ICustomCommand
    {
        #region Constants

        private const string VotingImage = "https://i2.wp.com/thevistapress.com/wp-content/uploads/2016/10/polling-research-firm-syracuse-ny.jpg?resize=200%2C125";

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly Config _config;
        private readonly IEventLogger _logger;

        private readonly List<string> _validVotingReactions = new List<string>
        {
            "🇦",
            "🇧",
            "🇨",
            "🇩",
            "🇪",
            "🇫",
            "🇬",
            "🇭",
            "🇮",
            "🇯",
            "🇰",
            "🇱",
            "🇲"
        };

        #endregion

        #region Properties

        public CommandPermissionLevel PermissionLevel => CommandPermissionLevel.Moderator;

        #endregion

        #region Constructor

        public CreateVoteCommand(DiscordClient client, Config config, IEventLogger logger)
        {
            _client = client;
            _config = config;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        public async Task Execute(DiscordMessage message, Command command)
        {
            if (command.Args.Count == 0) return;

            var question = command.Args[0];
            var answers = command.Args[1];

            var channel = await _client.GetChannel(_config.VotingPollsChannelId);
            if (channel == null)
            {
                _logger.Error($"Failed to get voting poll channel with id {_config.VotingPollsChannelId}.");
                return;
            }

            var poll = new VotingPoll
            {
                Question = question,
                Answers = new List<string>(answers.Trim('\0', ' ').Split(',')),
                Enabled = true
            };

            var eb = new DiscordEmbedBuilder { Title = poll.Question, Color = DiscordColor.Purple };
            for (int i = 0; i < poll.Answers.Count; i++)
            {
                eb.Description += $"{_validVotingReactions[i]} {poll.Answers[i]}\r\n\r\n";
            }

            eb.ImageUrl = VotingImage;

            var embed = eb.Build();
            if (embed == null) return;

            var pollMessage = await channel.SendMessageAsync(command.Args.Count == 3 ? command.Args[2] : string.Empty, false, embed);
            poll.PollMessageId = pollMessage.Id;

            for (int i = 0; i < poll.Answers.Count; i++)
            {
                await pollMessage.CreateReactionAsync(DiscordEmoji.FromName(_client, ":regional_indicator_" + (i + 1).NumberToAlphabet() + ":"));
            }
        }

        #endregion
    }

    public class VotingPoll
    {
        public string Question { get; set; }

        public List<string> Answers { get; set; }

        public Dictionary<ulong, string> Votes { get; set; }

        //public DateTime Started { get; set; }

        //public DateTime Ends { get; set; }

        public bool Enabled { get; set; }

        public ulong PollMessageId { get; set; }

        public VotingPoll()
        {
            Answers = new List<string>();
            Votes = new Dictionary<ulong, string>();
        }
    }
}