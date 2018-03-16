namespace BrockBot.Utilities
{
    using System;
    using System.Text;

    public static class Randomizer
    {
        private static readonly Random _rand;

        static Randomizer()
        {
            _rand = new Random();
        }

        /// <summary>
        /// Generates a random numeric value at a random length between 1000 - int.MaxValue.
        /// </summary>
        /// <returns>Returns a random number between 1000 - int.MaxValue.</returns>
        public static int RandomInt()
        {
            return RandomInt(1000, int.MaxValue);
        }

        /// <summary>
        /// Generates a random numeric value between the specified minimum and maximum values.
        /// </summary>
        /// <param name="min">Minimum expected result.</param>
        /// <param name="max">Maximum expected result.</param>
        /// <returns>Returns a random number between the specified minimum and maximum values.</returns>
        public static int RandomInt(int min, int max)
        {
            return _rand.Next(min, max);
        }

        /// <summary>
        /// Generates a random ASCII string at a random length between 1000 - int.MaxValue.
        /// </summary>
        /// <returns>Returns a random string.</returns>
        public static string RandomString()
        {
            return RandomString(RandomInt());
        }

        /// <summary>
        /// Generates a random ASCII string at the specified length.
        /// </summary>
        /// <param name="length">The length of the string to return.</param>
        /// <returns>Returns a random string.</returns>
        public static string RandomString(int length)
        {
            var sb = new StringBuilder();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789_";
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[_rand.Next(0, chars.Length)]);
            }
            return sb.ToString();
        }
    }
}