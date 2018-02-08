namespace BrockBot.Services.Geofence
{
    using System;
    using System.Collections.Generic;

    public class GeofenceService
    {
        private readonly List<GeofenceItem> _geofences;

        public GeofenceService(List<GeofenceItem> geofences)
        {
            _geofences = geofences;
        }

        public bool Contains(GeofenceItem geofence, Location point)
        {
            //Credits: https://stackoverflow.com/a/7739297/2313836

            var c = false;
            for (int i = 0, j = geofence.Polygons.Count - 1; i < geofence.Polygons.Count; j = i++)
            {
                if ((((geofence.Polygons[i].Latitude <= point.Latitude) && (point.Latitude < geofence.Polygons[j].Latitude))
                        || ((geofence.Polygons[j].Latitude <= point.Latitude) && (point.Latitude < geofence.Polygons[i].Latitude)))
                        && (point.Longitude < (geofence.Polygons[j].Longitude - geofence.Polygons[i].Longitude) * (point.Latitude - geofence.Polygons[i].Latitude)
                            / (geofence.Polygons[j].Latitude - geofence.Polygons[i].Latitude) + geofence.Polygons[i].Longitude))
                {
                    c = !c;
                }
            }
            return c;
        }

        public GeofenceItem GetGeofence(Location point)
        {
            foreach (var geofence in _geofences)
            {
                if (Contains(geofence, point))
                {
                    return geofence;
                }
            }

            return null;
        }
    }
}