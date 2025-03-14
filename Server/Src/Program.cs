using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Src
{
    public class Program
    {
        private const int Port = 5000;

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.Write("Choose protocol (TCP/UDP): ");
            var protocol = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (protocol != "TCP" && protocol != "UDP")
            {
                Console.WriteLine("Invalid protocol. Use TCP or UDP.");
                return;
            }

            var localIp = Utils.GetLocalIpAddress();
            Console.WriteLine($"Server started on {localIp}:{Port} using {protocol}");

            if (protocol == "TCP")
            {
                StartTcpServer();
            }
            else
            {
                StartUdpServer();
            }
        }

        private static void StartTcpServer()
        {
            var tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();

            while (true)
            {
                var client = tcpListener.AcceptTcpClient();
                var clientEndPoint = client.Client.RemoteEndPoint?.ToString();
                var ipAddress = clientEndPoint!.Split(':')[0];
                Console.WriteLine($"[TCP] Client connected from {ipAddress}");

                Task.Run(() => TcpClientHandler.HandleClient(client, ipAddress));
            }
        }

        private static void StartUdpServer()
        {
            var udpClient = new UdpClient(Port);

            while (true)
            {
                try
                {
                    var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var buffer = udpClient.Receive(ref remoteEndPoint);
                    var receivedMessage = Encoding.UTF8.GetString(buffer).TrimEnd('\r', '\n');
                    var ipAddress = remoteEndPoint.Address.ToString();

                    UdpClientHandler.HandleMessage(receivedMessage, ipAddress, remoteEndPoint, udpClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UDP] Error: {ex.Message}");
                }
            }
        }
    }
}
