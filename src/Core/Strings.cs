namespace BrockBot
{
    using System.Collections.Generic;

    public static class Strings
    {
        public const string GoogleMaps = "http://maps.google.com/maps?q={0},{1}";
        public const string GoogleMapsStaticImage = "https://maps.googleapis.com/maps/api/staticmap?center={0},{1}&markers=color:red%7C{0},{1}&maptype=roadmap&size=300x175&zoom=14";

        public const string PokemonImage = "https://ver.sx/pogo/monsters/{0:D3}_{1:D3}.png";

        public static IReadOnlyDictionary<int, string> WeatherEmojis => new Dictionary<int, string>
        {
            { 0, "☀️" },
            { 1, "☔️" },
            { 2, "⛅" },
            { 3, "☁️" },
            { 4, "💨" },
            { 5, "⛄️" },
            { 6, "🌁" }
        };

        public static IReadOnlyDictionary<string, string> TypeEmojis => new Dictionary<string, string>
        {
            { "normal", "⭕" },
            { "fighting", "🥋" },
            { "flying", "🐦" },
            { "poison", "☠" },
            { "ground", "⛰️" },
            { "dark", "💎" },
            { "bug", "🐛" },
            { "ghost", "👻" },
            { "steel", "⚙" },
            { "fire", "🔥" },
            { "water", "💧" },
            { "grass", "🍃" },
            { "electric", "⚡" },
            { "psychic", "🔮" },
            { "ice", "❄" },
            { "dragon", "🐲" },
            { "fairy", "💫" },
            { "rock", "🌑" }
        };

        //https://raw.githubusercontent.com/Necrobot-Private/PokemonGO-Assets/master/pokemon/{0}.png
        //https://github.com/not4profit/images/tree/master/monsters/{0:D3}.png
        //https://bytebucket.org/anzmap/sprites/raw/388a1e0ef08b98eaa0412c8a5f67ffb14d6a707d/{0}.png
    }
}