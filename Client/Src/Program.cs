using System.Net.Sockets;
using System.Text;
using ClientInfoLibrary;

namespace Client.Src
{
    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            Console.Write("Enter server IP: ");
            var serverIp = Console.ReadLine()?.Trim();

            const int port = 5000;

            try
            {
                using var client = new TcpClient(serverIp!, port);
                Console.WriteLine("Connected to server");

                var stream = client.GetStream();

                CommunicateWithServer.CheckResumableFiles(stream);

                CommunicateWithServer.Communicate(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
