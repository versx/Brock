namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Timers;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using BrockBot.Diagnostics;
    using BrockBot.Services.Geofence;

    public interface IWeatherService
    {
        //AccuWeatherCondition GetWeatherCondition(string city);
        WeatherData GetWeatherConditions(string city);

        List<WeatherData> GetWeatherConditions();
    }

    public class WeatherService : IWeatherService
    {
        //private readonly string _apiKey;
        private readonly IEventLogger _logger;
        private readonly Timer _timer;
        private readonly List<WeatherData> _weatherConditions;
        private readonly GeofenceService _geofenceSvc;

        #region Properties

        //public string AccuWeatherUrl => Debug ? "http://apidev.accuweather.com/" : "http://dataservice.accuweather.com/";

        //public bool Debug { get; set; }

        #endregion

        #region Constructor

        public WeatherService(/*string apiKey,*/GeofenceService geofenceSvc, IEventLogger logger)
        {
            //_apiKey = apiKey;
            _geofenceSvc = geofenceSvc;
            _logger = logger;
            //Debug = true;
            _weatherConditions = new List<WeatherData>();
            _timer = new Timer { Interval = (60 * 1000) * 15 };
            _timer.Elapsed += CheckWeatherConditionsEventHandler;
            _timer.Start();
        }

        public List<WeatherData> GetWeatherConditions()
        {
            var urls = new string[]
            {
                "https://pokemap.ver.sx/weather",
                "https://whittier.ver.sx/weather",
                "https://eastla.ver.sx/weather"
            };

            var list = new List<WeatherData>();
            foreach (var url in urls)
            {
                var json = MakeRequest(url);
                if (string.IsNullOrEmpty(json)) continue;

                var obj = JsonConvert.DeserializeObject<List<WeatherData>>(json);
                if (obj == null) continue;

                list.AddRange(obj);
            }

            return list;
        }

        public WeatherData GetWeatherConditions(string city)
        {
            var conditions = GetWeatherConditions();
            foreach (var condition in conditions)
            {
                var geofence = _geofenceSvc.GetGeofence(new Location(condition.Latitude, condition.Longitude));
                if (string.Compare(geofence.Name, city) == 0)
                {
                    return condition;
                }
            }

            return null;
        }

        private void CheckWeatherConditionsEventHandler(object sender, ElapsedEventArgs e)
        {
            _weatherConditions.Clear();
            _weatherConditions.AddRange(GetWeatherConditions());
        }

        #endregion

        #region Public Methods

        //public AccuWeatherCondition GetWeatherCondition(string city)
        //{
        //    var cityLocationCode = GetLocationCode(city);
        //    if (cityLocationCode == null) return null;

        //    var cityConditions = GetConditions(cityLocationCode);
        //    return cityConditions;
        //}

        #endregion

        #region Private Methods

        //private string GetLocationCode(string searchText)
        //{
        //    var url = $"{AccuWeatherUrl}locations/v1/search?q={searchText}&apikey={_apiKey}";
        //    var req = MakeRequest(url);
        //    if (string.IsNullOrEmpty(req)) return null;

        //    var accuWeatherLocation = JsonConvert.DeserializeObject<List<AccuWeatherLocation>>(req);
        //    if (accuWeatherLocation != null && accuWeatherLocation.Count > 0)
        //    {
        //        return accuWeatherLocation[0].Key;
        //    }

        //    return null;
        //}

        //private AccuWeatherCondition GetConditions(string locationKey, string language = "en")
        //{
        //    var url = $"{AccuWeatherUrl}currentconditions/v1/{locationKey}.json?language={language}&apikey={_apiKey}";
        //    var req = MakeRequest(url);
        //    if (string.IsNullOrEmpty(req)) return null;

        //    var accuWeatherCurrentConditions = JsonConvert.DeserializeObject<List<AccuWeatherCondition>>(req);
        //    if (accuWeatherCurrentConditions != null && accuWeatherCurrentConditions.Count > 0)
        //    {
        //        return accuWeatherCurrentConditions[0];
        //    }

        //    return null;
        //}

        private string MakeRequest(string url)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Proxy = null;
                    return wc.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to make request to {url} returned exception: {ex}");
                return null;
            }
        }

        #endregion
    }

    [JsonObject("weatherData")]
    public class WeatherData
    {
        public double Latitude
        {
            get { return Convert.ToDouble(Location != null ? Location.Split(',')[0] : "0"); }
        }

        public double Longitude
        {
            get { return Convert.ToDouble(Location != null ? Location.Split(',')[1] : "0"); }
        }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("gameplay_weather")]
        public WeatherCondition GameplayWeather { get; set; }

        [JsonProperty("cloud_level")]
        public int CloudLevel { get; set; }

        [JsonProperty("rain_level")]
        public int RainLevel { get; set; }

        [JsonProperty("wind_level")]
        public int WindLevel { get; set; }

        [JsonProperty("wind_direction")]
        public WindDirection WindDirection { get; set; }

        [JsonProperty("snow_level")]
        public int SnowLevel { get; set; }

        [JsonProperty("fog_level")]
        public int FogLevel { get; set; }

        [JsonProperty("severity")]
        public Severity Severity { get; set; }

        [JsonProperty("warn_weather")]
        public int WarnWeather { get; set; }

        [JsonProperty("last_updated")]
        public DateTime LastUpdated { get; set; }

        [JsonProperty("world_time")]
        public WorldTime WorldTime { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum WeatherCondition
    {
        Clear,
        Rainy,
        Partly_Cloudy,
        Overcast,
        Cloudy,
        Windy,
        Snow,
        Fog
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        None,
        Moderate,
        Extreme
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum WindDirection
    {
        N,
        NNE,
        NE,
        ENE,
        E,
        ESE,
        SE,
        SSE,
        S,
        SSW,
        SW,
        WSW,
        W,
        WNW,
        NW,
        NNW
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorldTime
    {
        Day,
        Night
    }

    //public class AccuWeatherCondition
    //{
    //    public DateTime LocalObservationDateTime { get; set; }
    //    public int EpochTime { get; set; }
    //    public string WeatherText { get; set; }
    //    public int WeatherIcon { get; set; }
    //    public bool IsDayTime { get; set; }
    //    public Temperature Temperature { get; set; }
    //    public string MobileLink { get; set; }
    //    public string Link { get; set; }
    //}

    //public class AccuWeatherLocation
    //{
    //    public string Key { get; set; }
    //}

    //public class Temperature
    //{
    //    public Metric Metric { get; set; }
    //    public Imperial Imperial { get; set; }
    //}

    //public class Metric
    //{
    //    public double Value { get; set; }
    //    public string Unit { get; set; }
    //    public int UnitType { get; set; }
    //}

    //public class Imperial
    //{
    //    public double Value { get; set; }
    //    public string Unit { get; set; }
    //    public int UnitType { get; set; }
    //}
}