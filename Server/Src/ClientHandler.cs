using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class ClientHandler(TcpClient client)
    {
        public void HandleClient()
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            while (true)
            {
                try
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    var request = Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimEnd('\r', '\n');
                    Console.WriteLine($"Received command: {request}");

                    var response = CommandProcessor.ProcessCommand(request, client);
                    var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                    stream.Write(responseBytes, 0, responseBytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    break;
                }
            }

            client.Close();
            Console.WriteLine("Client disconnected");
        }
    }
}