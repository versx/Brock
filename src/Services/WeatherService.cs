namespace BrockBot.Services
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    using Newtonsoft.Json;

    using BrockBot.Diagnostics;

    public interface IWeatherService
    {
        AccuWeatherCondition GetWeatherCondition(string city);
    }

    public class WeatherService : IWeatherService
    {
        private readonly string _apiKey;
        private readonly IEventLogger _logger;

        #region Properties

        public string AccuWeatherUrl => Debug ? "http://apidev.accuweather.com/" : "http://dataservice.accuweather.com/";

        public bool Debug { get; set; }

        #endregion

        #region Constructor

        public WeatherService(string apiKey, IEventLogger logger)
        {
            _apiKey = apiKey;
            _logger = logger;
            //Debug = true;
        }

        #endregion

        #region Public Methods

        public AccuWeatherCondition GetWeatherCondition(string city)
        {
            var cityLocationCode = GetLocationCode(city);
            if (cityLocationCode == null) return null;

            var cityConditions = GetConditions(cityLocationCode);
            return cityConditions;
        }

        #endregion

        #region Private Methods

        private string GetLocationCode(string searchText)
        {
            var url = $"{AccuWeatherUrl}locations/v1/search?q={searchText}&apikey={_apiKey}";
            var req = MakeRequest(url);
            if (string.IsNullOrEmpty(req)) return null;

            var accuWeatherLocation = JsonConvert.DeserializeObject<List<AccuWeatherLocation>>(req);
            if (accuWeatherLocation != null && accuWeatherLocation.Count > 0)
            {
                return accuWeatherLocation[0].Key;
            }

            return null;
        }

        private AccuWeatherCondition GetConditions(string locationKey, string language = "en")
        {
            var url = $"{AccuWeatherUrl}currentconditions/v1/{locationKey}.json?language={language}&apikey={_apiKey}";
            var req = MakeRequest(url);
            if (string.IsNullOrEmpty(req)) return null;

            var accuWeatherCurrentConditions = JsonConvert.DeserializeObject<List<AccuWeatherCondition>>(req);
            if (accuWeatherCurrentConditions != null && accuWeatherCurrentConditions.Count > 0)
            {
                return accuWeatherCurrentConditions[0];
            }

            return null;
        }

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

    public class AccuWeatherCondition
    {
        public DateTime LocalObservationDateTime { get; set; }
        public int EpochTime { get; set; }
        public string WeatherText { get; set; }
        public int WeatherIcon { get; set; }
        public bool IsDayTime { get; set; }
        public Temperature Temperature { get; set; }
        public string MobileLink { get; set; }
        public string Link { get; set; }
    }

    public class AccuWeatherLocation
    {
        public string Key { get; set; }
    }

    public class Temperature
    {
        public Metric Metric { get; set; }
        public Imperial Imperial { get; set; }
    }

    public class Metric
    {
        public double Value { get; set; }
        public string Unit { get; set; }
        public int UnitType { get; set; }
    }

    public class Imperial
    {
        public double Value { get; set; }
        public string Unit { get; set; }
        public int UnitType { get; set; }
    }
}