namespace BrockBot.Utilities
{
    using System;
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
    }
}