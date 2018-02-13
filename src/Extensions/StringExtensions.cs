namespace BrockBot.Extensions
{
    using System;

    public static class StringExtensions
    {
        public static bool TryParse(this string weather, out WeatherType result)
        {
            try
            {
                return Enum.TryParse(weather, true, out result);
            }
            catch
            {
                result = WeatherType.Clear;
                return false;
            }
        }
    }
}