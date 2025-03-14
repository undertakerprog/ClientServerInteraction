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

            Console.WriteLine("TCP server started. Waiting for connections...");

            var pendingClients = new List<TcpClient>();

            while (true)
            {
                if (tcpListener.Pending())
                {
                    var client = tcpListener.AcceptTcpClient();
                    var clientEndPoint = client.Client.RemoteEndPoint?.ToString();
                    var ipAddress = clientEndPoint!.Split(':')[0];
                    Console.WriteLine($"[TCP] Client connected from {ipAddress}");

                    pendingClients.Add(client);

                    Console.Write($"[ADMIN]Accept the client - {ipAddress}?('ACCEPT'/'SKIP'): ");
                }

                if (Console.KeyAvailable)
                {
                    var command = Console.ReadLine()?.Trim().ToUpper();

                    switch (command)
                    {
                        case "ACCEPT" when pendingClients.Count > 0:
                            {
                                var client = pendingClients[0];
                                pendingClients.RemoveAt(0);

                                var clientEndPoint = client.Client.RemoteEndPoint?.ToString();
                                var ipAddress = clientEndPoint!.Split(':')[0];

                                var acceptMessage = "accept\r\n"u8.ToArray();
                                var stream = client.GetStream();
                                stream.Write(acceptMessage, 0, acceptMessage.Length);

                                Task.Run(() => TcpClientHandler.HandleClient(client, ipAddress));
                                Console.WriteLine($"[TCP] Client {ipAddress} is now being processed.");
                                break;
                            }
                        case "SKIP" when pendingClients.Count > 0:
                            {
                                var skippedClient = pendingClients[0];
                                pendingClients.RemoveAt(0);

                                var skippedClientEndPoint = skippedClient.Client.RemoteEndPoint?.ToString();
                                var skippedIpAddress = skippedClientEndPoint!.Split(':')[0];

                                var skipMessage = "skip\r\n"u8.ToArray();
                                var stream = skippedClient.GetStream();
                                stream.Write(skipMessage, 0, skipMessage.Length);

                                Console.WriteLine($"[TCP] Client {skippedIpAddress} has been skipped.");
                                skippedClient.Close();
                                break;
                            }
                        default:
                            Console.WriteLine("Invalid command. Please type 'TAKE' to accept the client or 'SKIP' to ignore.");
                            break;
                    }
                }

                Thread.Sleep(10);
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
