namespace BrockBot.Extensions
{
    using System;

    public static class IntegerExtensions
    {
        public static char NumberToAlphabet(this int num, bool caps = false)
        {
            return Convert.ToChar(num + (caps ? 64 : 96));
        }
    }
}