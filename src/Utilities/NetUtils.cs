namespace BrockBot.Utilities
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    public static class NetUtils
    {
        public static string GetLocalIPv4()
        {
            return GetLocalIPv4(NetworkInterfaceType.Ethernet) ??
                   GetLocalIPv4(NetworkInterfaceType.Wireless80211);
        }

        public static string GetLocalIPv4(NetworkInterfaceType type)
        {
            var result = string.Empty;
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                var netInterface = interfaces[i];
                var isUp = netInterface.NetworkInterfaceType == type && 
                           netInterface.OperationalStatus == OperationalStatus.Up;
                if (!isUp) continue;

                foreach (var ipAddress in netInterface.GetIPProperties().UnicastAddresses)
                {
                    var isIpv4 = ipAddress.Address.AddressFamily == AddressFamily.InterNetwork;
                    if (isIpv4)
                    {
                        result = ipAddress.Address.ToString();
                        break;
                    }
                }
            }
            return result;
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
                Utils.LogError(ex);
            }
        }
    }
}