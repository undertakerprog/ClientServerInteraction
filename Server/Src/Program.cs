using ClientInfoLibrary;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Src
{
    public class Program
    {
        private const int Port = 5000;
        private static readonly ClientManager ClientManager = new();

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

            var clients = new List<TcpClient>();

            while (true)
            {
                if (tcpListener.Pending())
                {
                    var client = tcpListener.AcceptTcpClient();
                    var clientEndPoint = client.Client.RemoteEndPoint?.ToString();
                    var ipAddress = clientEndPoint!.Split(':')[0];
                    Console.WriteLine($"[TCP] Client connected from {ipAddress}");

                    clients.Add(client);

                    var stream = client.GetStream();
                    if (ClientManager.CanDownloadFile(ipAddress) && ClientManager.GetFileName(ipAddress) != "No File")
                    {
                        var fileName = ClientManager.GetFileName(ipAddress) + "\r\n";
                        var fileNameBytes = Encoding.UTF8.GetBytes("RESUME " + fileName);
                        stream.Write(fileNameBytes, 0, fileNameBytes.Length);
                    }
                    else
                    {
                        var skipMessage = "skip\r\n"u8.ToArray();
                        stream.Write(skipMessage, 0, skipMessage.Length);
                    }
                }

                for (var i = clients.Count - 1; i >= 0; i--)
                {
                    var client = clients[i];
                    var stream = client.GetStream();

                    if (!stream.DataAvailable) continue;
                    try
                    {
                        var buffer = new byte[1024];
                        var bytesRead = stream.Read(buffer, 0, buffer.Length);

                        if (bytesRead == 0)
                        {
                            Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} disconnected.");
                            clients.RemoveAt(i);
                            client.Close();
                            continue;
                        }

                        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
                        var clientIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                        Console.WriteLine($"[TCP - {clientIp}] Received command: {request}");

                        var response = CommandProcessor.ProcessCommand(request, client, ClientManager);
                        var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                        stream.Write(responseBytes, 0, responseBytes.Length);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling client: {ex.Message}");
                        clients.RemoveAt(i);
                        client.Close();
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
