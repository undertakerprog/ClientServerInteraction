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

            Console.Write("Enter server IP: ");
            var serverIp = Console.ReadLine()?.Trim();

            try
            {
                if (TryConnectTcp(serverIp!))
                {
                    return;
                }
                ConnectUdp(serverIp!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static bool TryConnectTcp(string serverIp)
        {
            try
            {
                using var client = new TcpClient(serverIp, Port);
                Console.WriteLine("Connected to server (TCP)");

                var stream = client.GetStream();
                CommunicateWithServer.Communicate(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void ConnectUdp(string serverIp)
        {
            using var udpClient = new UdpClient();
            udpClient.Connect(serverIp, Port);

            var localEndPoint = udpClient.Client.LocalEndPoint as IPEndPoint;
            var clientIp = localEndPoint?.Address.ToString() ?? "UNKNOWN";

            var initialMessage = Encoding.UTF8.GetBytes($"CONNECTED {clientIp}");
            udpClient.Send(initialMessage, initialMessage.Length);

            Console.WriteLine("Connected to server (UDP)");

            while (true)
            {
                Console.Write("Enter message (or 'exit' to quit): ");
                var message = Console.ReadLine();
                if (message?.ToLower() == "exit")
                    break;

                if (message == null) continue;
                var request = Encoding.UTF8.GetBytes(message);
                udpClient.Send(request, request.Length);
            }
        }
    }
}
