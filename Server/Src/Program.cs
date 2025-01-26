using System.Net;
using System.Net.Sockets;

namespace Server.Src
{
    public class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            const int port = 5000;
            var localIp = Utils.GetLocalIpAddress();

            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine($"Server was create {localIp}:{port}. Wait for connections");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                Console.WriteLine("Client was connected");

                var clientHandler = new ClientHandler(client);
                clientHandler.HandleClient();
            }
        }
    }
}
