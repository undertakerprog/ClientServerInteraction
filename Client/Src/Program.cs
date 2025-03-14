using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public static class Program
    {
        private const int Port = 5000;

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.Write("Enter server IP in (format 8.8.8.8 -tcp) or (8.8.8.8): ");
            var input = Console.ReadLine()?.Trim();

            try
            {
                Debug.Assert(input != null, nameof(input) + " != null");
                var (serverIp, protocol) = ParseInput(input);

                if (string.IsNullOrEmpty(protocol)) return;
                switch (protocol)
                {
                    case "-tcp" when ConnectTcp(serverIp):
                        return;
                    case "-tcp":
                        Console.WriteLine("Failed to connect via TCP.");
                        break;
                    case "-udp":
                        ConnectUdp(serverIp);
                        return;
                    default:
                        Console.WriteLine("Invalid protocol. Use -tcp or -udp.");
                        return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static (string serverIp, string protocol) ParseInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new ArgumentException("Input cannot be empty.");
            }

            var parts = input.Split([' '], StringSplitOptions.RemoveEmptyEntries);

            return parts.Length switch
            {
                1 => (parts[0], string.Empty),
                2 => (parts[0], parts[1].ToLower()),
                _ => throw new ArgumentException("Invalid input format. Use: address -protocol(8.8.8.8 -tcp)")
            };
        }

        private static bool ConnectTcp(string serverIp)
        {
            try
            {
                using var client = new TcpClient(serverIp, Port);
                Console.WriteLine("Connected to server (TCP)");

                var stream = client.GetStream();

                var buffer = new byte[1024];
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                var initialMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                switch (initialMessage)
                {
                    case "skip":
                        Console.WriteLine("Server rejected the connection. Exiting...");
                        return false;
                    case "accept":
                        Console.WriteLine("Server accepted the connection.");
                        CommunicateWithServer.Communicate(stream);
                        return true;
                    default:
                        Console.WriteLine("Unknown response from server. Exiting...");
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }


        private static void ConnectUdp(string serverIp)
        {
            using var udpClient = new UdpClient();

            if (IPAddress.TryParse(serverIp, out var parsedAddress) == false)
            {
                var addresses = Dns.GetHostAddresses(serverIp);
                parsedAddress = addresses[1];
            }

            var serverEndPoint = new IPEndPoint(parsedAddress, Port);

            var localEndPoint = udpClient.Client.LocalEndPoint as IPEndPoint;
            var clientIp = localEndPoint?.Address.ToString() ?? "UNKNOWN";

            var initialMessage = Encoding.UTF8.GetBytes($"CONNECTED {clientIp}");
            udpClient.Send(initialMessage, initialMessage.Length, serverEndPoint);

            Console.WriteLine("Connected to server (UDP)");

            CommunicateWithServer.Communicate(udpClient, serverEndPoint);
        }
    }
}
