namespace BrockBot
{
    public class Location
    {
        //public string Address { get; }

        //public string City { get; }

        public double Latitude { get; }

        public double Longitude { get; }

        public Location(/*string address, string city,*/ double lat, double lng)
        {
            //Address = address;
            //City = city;
            Latitude = lat;
            Longitude = lng;
        }
    }
}