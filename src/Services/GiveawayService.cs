namespace BrockBot.Services
{
    using System;
    using System.IO;
    using System.Timers;

    using DSharpPlus;

    using BrockBot.Configuration;
    using BrockBot.Extensions;

    public class GiveawayService
    {
        #region Constants

        private const int MinPokemon = 1;
        private const int MaxPokemon = 387;

        #endregion

        #region Variables

        private readonly DiscordClient _client;
        private readonly ulong _giveawayChannelId;
        private readonly Timer _timer;
        private readonly Config _config;
        private uint _pokemonId;

        #endregion

        #region Constructor

        public GiveawayService(DiscordClient client, ulong giveawayChannelId, Config config)
        {
            _client = client;
            _giveawayChannelId = giveawayChannelId;
            _config = config;

            _timer = new Timer();
            _timer.Elapsed += OnElapsed;
        }

        #endregion

        #region Public Methods

        public void Start(DateTime startTime, uint pokeId = 0)
        {
            _config.Giveaways.Clear();

            if (pokeId == 0)
            {
                _pokemonId = (uint)BrockBot.Utilities.Utils.RandomInt(MinPokemon, MaxPokemon);
                while (_config.Giveaways.Exists(x => x.PokemonId == _pokemonId))
                {
                    _pokemonId = (uint)BrockBot.Utilities.Utils.RandomInt(MinPokemon, MaxPokemon);
                }
            }
            else
            {
                _pokemonId = pokeId;
            }

            Console.WriteLine($"Pokemon to guess is {_pokemonId}");
            File.AppendAllText("giveaway_answers.txt", $"Pokemon to guess is {_pokemonId}\r\n");

            if (_config.Giveaways.Exists(x => x.PokemonId == _pokemonId && x.StartTime == startTime))
            {
                Console.WriteLine($"Giveaway with pokemon id {_pokemonId} and start time {startTime} already exists.");
                return;
            }

            if (!_timer.Enabled)
            {
                _timer.Interval = startTime.Subtract(DateTime.Now).TotalMilliseconds - TimeSpan.FromSeconds(10).TotalMilliseconds;
                _timer.Start();
            }

            _config.Giveaways.Add(new Giveaway { PokemonId = _pokemonId, StartTime = startTime });
            _config.Save();
        }

        #endregion

        #region Private Methods

        private async void OnElapsed(object sender, ElapsedEventArgs e)
        {
            var channel = await _client.GetChannel(_giveawayChannelId);
            if (channel == null)
            {
                Console.WriteLine("ERROR: Failed to find giveaway channel, aborting...");
                return;
            }

            Console.WriteLine("The giveaway is starting with a countdown of 10 seconds...");
            await channel.SendMessageAsync($"{channel.Guild.EveryoneRole.Mention}, The giveaway is starting with a countdown of 10 seconds, here we go...");

            Console.WriteLine("10");
            await channel.SendMessageAsync("10");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("9");
            await channel.SendMessageAsync("9");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("8");
            await channel.SendMessageAsync("8");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("7");
            await channel.SendMessageAsync("7");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("6");
            await channel.SendMessageAsync("6");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("5");
            await channel.SendMessageAsync("5");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("4");
            await channel.SendMessageAsync("4");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("3");
            await channel.SendMessageAsync("3");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("2");
            await channel.SendMessageAsync("2");
            System.Threading.Thread.Sleep(1000);

            Console.WriteLine("1");
            await channel.SendMessageAsync("1");
            System.Threading.Thread.Sleep(1000);

            await channel.UnlockChannel(channel.Guild.EveryoneRole);

            Console.WriteLine("Channel is unlocked, GO!");
            await channel.SendMessageAsync("Channel is unlocked, GO!");

            if (_config.Giveaways.Exists(x => x.PokemonId == _pokemonId))
            {
                var giveaway = _config.Giveaways.Find(x => x.PokemonId == _pokemonId);
                giveaway.Started = true;
            }

            _timer.Stop();
            _timer.Dispose();
        }

        #endregion
    }
}