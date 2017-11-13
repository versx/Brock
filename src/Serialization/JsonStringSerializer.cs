namespace BrockBot.Serialization
{
    using Newtonsoft.Json;

    /// <summary>
    /// Json string serializer class wrapping the Newtonsoft.Json library.
    /// </summary>
    public static class JsonStringSerializer
    {
        /// <summary>
        /// Serializes an object to a json string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>Returns the serialized json string.</returns>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented).Replace("\"NULL\"", "null");
        }

        /// <summary>
        /// Deserializes the json string to an object.
        /// </summary>
        /// <param name="json">The json string to deserialize.</param>
        /// <returns>Returns the deserialized object.</returns>
        public static object Deserialize(string json)
        {
            return JsonConvert.DeserializeObject(json);
        }

        /// <summary>
        /// Deserializes the json string to an object.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the object as.</typeparam>
        /// <param name="json">The json string to deserialize.</param>
        /// <returns>Returns the deserialized object.</returns>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}