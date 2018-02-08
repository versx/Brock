namespace BrockBot.Services.Geofence
{
    public class Location
    {
        public double Latitude { get; }

        public double Longitude { get; }

        public Location(double lat, double lng)
        {
            Latitude = lat;
            Longitude = lng;
        }
    }
}