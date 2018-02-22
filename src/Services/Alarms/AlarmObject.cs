namespace BrockBot.Services.Alarms
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    using BrockBot.Services.Geofence;

    [JsonObject("alarm")]
    public class AlarmObject
    {
        [JsonIgnore]
        public GeofenceItem Geofence { get; private set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("filters")]
        public List<FilterObject> Filters { get; set; }

        [JsonProperty("geofenceFile")]
        public string GeofenceFile { get; set; }

        [JsonProperty("webhook")]
        public string Webhook { get; set; }

        public AlarmObject()
        {
            if (!string.IsNullOrEmpty(GeofenceFile))
            {
                ParseGeofenceFile();
            }
        }

        public void ParseGeofenceFile()
        {
            Geofence = GeofenceItem.FromFile(GeofenceFile);
        }
    }
}