using System.Net.Sockets;
using System.Net;

namespace Server.Src
{
    public class Utils
    {
        public static string GetLocalIpAddress()
        {
            var localIp = "IP not found";
            try
            {
                localIp = Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    ?.ToString() ?? "IP not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error to get IP: {ex.Message}");
            }
            return localIp;
        }
    }
}
