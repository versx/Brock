namespace BrockBot.Utilities
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public static class Utils
    {
        private static Random _rand;

        static Utils()
        {
            _rand = new Random();
        }

        /// <summary>
        /// Generates a random numeric value at a random length between 1000 - int.MaxValue.
        /// </summary>
        /// <returns>Returns a random number between 1000 - int.MaxValue.</returns>
        public static int RandomInt()
        {
            return RandomInt(1000, int.MaxValue);
        }

        /// <summary>
        /// Generates a random numeric value between the specified minimum and maximum values.
        /// </summary>
        /// <param name="min">Minimum expected result.</param>
        /// <param name="max">Maximum expected result.</param>
        /// <returns>Returns a random number between the specified minimum and maximum values.</returns>
        public static int RandomInt(int min, int max)
        {
            return _rand.Next(min, max);
        }

        /// <summary>
        /// Generates a random ASCII string at a random length between 1000 - int.MaxValue.
        /// </summary>
        /// <returns>Returns a random string.</returns>
        public static string RandomString()
        {
            return RandomString(RandomInt());
        }

        /// <summary>
        /// Generates a random ASCII string at the specified length.
        /// </summary>
        /// <param name="length">The length of the string to return.</param>
        /// <returns>Returns a random string.</returns>
        public static string RandomString(int length)
        {
            var sb = new StringBuilder();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789_";
            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[_rand.Next(0, chars.Length)]);
            }
            return sb.ToString();
        }

        public static void MakeWebRequest(string url, string payload)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = payload.Length;
            using (var webStream = request.GetRequestStream())
            using (var requestWriter = new StreamWriter(webStream, System.Text.Encoding.ASCII))
            {
                requestWriter.Write(payload);
            }

            try
            {
                var webResponse = request.GetResponse();
                using (var webStream = webResponse.GetResponseStream())
                {
                    if (webStream != null)
                    {
                        using (var responseReader = new StreamReader(webStream))
                        {
                            string response = responseReader.ReadToEnd();
                            Console.WriteLine("Response: {0}", response);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

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

        public static dynamic GetWebHookData(string webHook)
        {
            /**Example:
             * {
             *   "name": "Pogo", 
             *   "channel_id": "352137087182416016", 
             *   "token": "fCdHsCZWeGB_vTkdPRqnB4_7fXil5tutXDLAZQYDurkXWQOqzSptiSQHbiCOBGlsF8J8", 
             *   "avatar": null, 
             *   "guild_id": "322025055510855680", 
             *   "id": "352156775101032449"
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

        public static string ToReadableString(TimeSpan span, bool shortForm = false)
        {
            string formatted = string.Format
            (
                "{0}{1}{2}{3}",
                span.Duration().Days > 0 ? shortForm ? string.Format("{0:0}d, ", span.Days) : string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? "" : "s") : "",
                span.Duration().Hours > 0 ? shortForm ? string.Format("{0:0}h, ", span.Hours) : string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? "" : "s") : "",
                span.Duration().Minutes > 0 ? shortForm ? string.Format("{0:0}m, ", span.Minutes) : string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? "" : "s") : "",
                span.Duration().Seconds > 0 ? shortForm ? string.Format("{0:0}s", span.Seconds) : string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? "" : "s") : ""
            );

            if (formatted.EndsWith(", ", StringComparison.Ordinal))
                formatted = formatted.Substring(0, formatted.Length - 2);

            if (string.IsNullOrEmpty(formatted))
                formatted = "0 seconds";

            return formatted;
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
            return localDateTime;
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