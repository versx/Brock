namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class Geofence
    {
        public string Name { get; set; }

        public List<Location> Polygons { get; }

        public Geofence()
        {
            Name = "Unnamed";
            Polygons = new List<Location>();
        }

        public Geofence(string name)
        {
            Name = name;
            Polygons = new List<Location>();
        }

        public Geofence(string name, List<Location> polygons) : this()
        {
            Name = name;
            Polygons = polygons;
        }

        public static Geofence FromFile(string filePath)
        {
            var geofence = new Geofence();
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                if (line.StartsWith("[", StringComparison.Ordinal))
                {
                    geofence.Name = line.TrimStart('[').TrimEnd(']');
                    continue;
                }

                var coordinates = line.Replace(" ", null).Split(',');
                var lat = double.Parse(coordinates[0]);
                var lng = double.Parse(coordinates[1]);

                geofence.Polygons.Add(new Location(lat, lng));
            }

            return geofence;
        }

        public static List<Geofence> FromFiles(List<string> filePaths)
        {
            var list = new List<Geofence>();

            foreach (var filePath in filePaths)
            {
                list.Add(FromFile(filePath));
            }

            return list;
        }

        public static List<Geofence> Load(string geofenceFolder, List<string> cities)
        {
            var list = new List<Geofence>();
            foreach (var city in cities)
            {
                var filePath = Path.Combine(geofenceFolder, city + ".txt");
                if (File.Exists(filePath))
                {
                    list.Add(FromFile(filePath));
                }
            }
            return list;
        }
    }
}