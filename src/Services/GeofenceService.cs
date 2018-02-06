namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;

    public class GeofenceService
    {
        private readonly List<Geofence> _geofences;

        public GeofenceService(List<Geofence> geofences)
        {
            _geofences = geofences;
        }

        public bool Contains(Geofence geofence, Location point)
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

        public Geofence GetGeofence(Location point)
        {
            //Credits: https://stackoverflow.com/a/7739297/2313836

            var c = false;
            for (int i = 0; i < _geofences.Count; i++)
            {
                for (int j = 0, k = _geofences[i].Polygons.Count - 1; j < _geofences[i].Polygons.Count; k = j++)
                {
                    if ((((_geofences[i].Polygons[j].Latitude <= point.Latitude) && (point.Latitude < _geofences[i].Polygons[k].Latitude))
                            || ((_geofences[i].Polygons[k].Latitude <= point.Latitude) && (point.Latitude < _geofences[i].Polygons[j].Latitude)))
                            && (point.Longitude < (_geofences[i].Polygons[k].Longitude - _geofences[i].Polygons[j].Longitude) * (point.Latitude - _geofences[i].Polygons[j].Latitude)
                                / (_geofences[i].Polygons[k].Latitude - _geofences[i].Polygons[j].Latitude) + _geofences[i].Polygons[j].Longitude))
                    {
                        c = !c;
                    }
                }

                if (c) return _geofences[i];
                c = false;
            }

            return null;
        }
    }
}