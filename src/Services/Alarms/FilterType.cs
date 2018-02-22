﻿namespace BrockBot.Services.Alarms
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FilterType
    {
        Include = 0,
        Exclude
    }
}