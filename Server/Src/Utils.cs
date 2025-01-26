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
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    localIp = ip.ToString();
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error to get ip {ex.Message}");
            }
            return localIp;
        }
    }
}
