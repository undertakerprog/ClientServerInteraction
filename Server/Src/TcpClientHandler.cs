using System.Net.Sockets;
using System.Text;
using ClientInfoLibrary;

namespace Server
{
    public class TcpClientHandler()
    {
        private readonly TcpClient? _tcpClient;

        public static readonly ClientManager ClientManager = new();

        public TcpClientHandler(TcpClient client) : this()
        {
            _tcpClient = client;
        }

        public void TcpHandleClient(string ipAddress)
        {
            var stream = _tcpClient!.GetStream();
            var buffer = new byte[1024];

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

            while (true)
            {
                try
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
                    Console.WriteLine($"[TCP] Received command: {request}");

                    var response = CommandProcessor.ProcessCommand(request, _tcpClient, ClientManager);
                    var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }

            _tcpClient.Close();
            Console.WriteLine("Client disconnected");
        }
    }
}