namespace BrockBot.Utilities
{
    using System;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public static class Utils
    {
        public static string GetBetween(string data, string start, string end)
        {
            try
            {
                int pFrom = data.IndexOf(start, StringComparison.Ordinal) + start.Length;
                int pTo = data.LastIndexOf(end, StringComparison.Ordinal);

                string result = data.Substring(pFrom, pTo - pFrom);
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return data;
            }
        }

        public static string GetLastLine(string text)
        {
            var match = Regex.Match(text, "^.*$", RegexOptions.Multiline | RegexOptions.RightToLeft);
            if (match.Success)
            {
                return match.Value;
            }

            return string.Empty;
        }

        public static dynamic GetWebHookData(string webHook)
        {
            /**Example:
             * {
             *   "name": "Pogo", 
             *   "channel_id": "352137087182416026", 
             *   "token": "fCdHsCZWeGB_vTkdPRqnB4_7fXil5tutXDLAZQYDurkXWQOqzSptiSQHbiCOBGlsg8J8", 
             *   "avatar": null, 
             *   "guild_id": "322025055510854680", 
             *   "id": "352156775101032439"
             * }
             * 
             */

            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                string json = wc.DownloadString(webHook);
                dynamic data = JsonConvert.DeserializeObject(json);
                return data;
            }
        }

        public static void LogError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine("ERROR:");
            Console.WriteLine(ex);
            Console.WriteLine();
            Console.ResetColor();
        }

        /// <summary>
        /// Asynchronous delay the specified amount of time in milliseconds. 
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <returns>Returns Task.</returns>
        public static async Task Wait(int timeoutMs)
        {
            await Task.Delay(timeoutMs);
        }

        public static double ToUnix(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixDateTime = (dateTime.ToUniversalTime() - epoch).TotalSeconds;
            return unixDateTime;
        }

        public static DateTime FromUnix(double unixSeconds)
        {
            var timeSpan = TimeSpan.FromSeconds(unixSeconds);
            var localDateTime = new DateTime(timeSpan.Ticks).ToLocalTime();

            //return localDateTime.AddHours(Convert.ToInt32(localDateTime.IsDaylightSavingTime()));
            return localDateTime.AddHours(1);
        }

        public static Version GetPoGoApiVersion()
        {
            try
            {
                const string url = "https://pgorelease.nianticlabs.com/plfe/version";
                using (var wc = new WebClient())
                {
                    var ver = wc.DownloadString(url);
                    ver = ver.Trim('\0', '\r', '\n');

                    return new Version(ver.Substring(1, ver.Length - 1));
                }
            }
            catch
            {
                return new Version(0, 0);
            }
        }

        //public static Location GetGoogleAddress(double lat, double lng, string gmapsKey)
        //{
        //    var url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={lat},{lng}&sensor=true&key={gmapsKey}";

        //    try
        //    {
        //        var request = (HttpWebRequest)WebRequest.Create(url);
        //        var response = request.GetResponse();
        //        using (var responseStream = response.GetResponseStream())
        //        {
        //            var reader = new StreamReader(responseStream, Encoding.UTF8);
        //            var data = reader.ReadToEnd();
        //            var parseJson = JObject.Parse(data);

        //            if (Convert.ToString(parseJson["status"]) != "OK") return null;

        //            var jsonres = parseJson["results"][0];
        //            var address = Convert.ToString(jsonres["formatted_address"]);
        //            var addrComponents = jsonres["address_components"];
        //            var city = "Unknown";

        //            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(addrComponents.ToString());

        //            foreach (var item in items)
        //            {
        //                foreach (var key in item)
        //                {
        //                    if (key.Key == "types")
        //                    {
        //                        if (key.Value is JArray types)
        //                        {
        //                            foreach (var type in types)
        //                            {
        //                                var t = type.ToString();
        //                                if (string.Compare(t, "locality", true) == 0)
        //                                {
        //                                    city = Convert.ToString(item["short_name"]);
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }

        //                    if (city != "Unknown") break;
        //                }

        //                if (city != "Unknown") break;
        //            }

        //            //File.AppendAllText("cities.txt", $"City: {city}\r\n");

        //            return new Location(address, city, lat, lng);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogError(ex);
        //    }

        //    return null;
        //}
    }
}