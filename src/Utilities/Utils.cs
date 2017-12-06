namespace BrockBot.Utilities
{
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public static class Utils
    {
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
                return null;
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

        public static string ToReadableString(TimeSpan span)
        {
            string formatted = string.Format
            (
                "{0}{1}{2}{3}",
                span.Duration().Days > 0 ? string.Format("{0:0} day{1}, ", span.Days, span.Days == 1 ? "" : "s") : "",
                span.Duration().Hours > 0 ? string.Format("{0:0} hour{1}, ", span.Hours, span.Hours == 1 ? "" : "s") : "",
                span.Duration().Minutes > 0 ? string.Format("{0:0} minute{1}, ", span.Minutes, span.Minutes == 1 ? "" : "s") : "",
                span.Duration().Seconds > 0 ? string.Format("{0:0} second{1}", span.Seconds, span.Seconds == 1 ? "" : "s") : ""
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
    }
}