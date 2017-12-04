namespace BrockBot.Extensions
{
    using System;

    public static class TypeExtensions
    {
        public static object[] GetAttributes<T>(this Type type)
        {
            return type.GetCustomAttributes(typeof(T), false);
        }

        public static T GetAttribute<T>(this Type type)
        {
            return (T)GetAttributes<T>(type)[0];
        }
    }
}