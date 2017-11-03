namespace PokeFilterBot.Serialization
{
    using System;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>
    /// Xml string serializer class wrapping the native <see cref="XmlSerializer"/>.
    /// </summary>
    public static class XmlStringSerializer
    {
        /// <summary>
        /// Serializes an object to an xml string.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>Returns the serialized xml string.</returns>
        public static string Serialize<T>(T obj)
        {
            var xs = new XmlSerializer(typeof(T));
            using (StringWriter sw = new StringWriter())
            {
                xs.Serialize(sw, obj);
                return sw.ToString();
            }
        }

        /// <summary>
        /// Deserialize an xml string to an object.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <param name="data">The xml string to deserialize.</param>
        /// <returns>Returns the deserialized object.</returns>
        public static T Deserialize<T>(string data)
        {
            var xs = new XmlSerializer(typeof(T));
            using (StringReader sr = new StringReader(data))
            {
                var obj = (T)xs.Deserialize(sr);
                return obj;
            }
        }
    }
}