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

            if (File.Exists("clients.json"))
            {
                const string question = "Do you want to continue? (yes/no)\r\n";
                var questionBytes = Encoding.UTF8.GetBytes(question);
                stream.Write(questionBytes, 0, questionBytes.Length);

                var clientRead = stream.Read(buffer, 0, buffer.Length);
                var clientResponse = Encoding.UTF8.GetString(buffer, 0, clientRead).Trim();

                if (clientResponse.ToLower() == "yes")
                {
                    Console.WriteLine("Client chose to continue.");
                }
                else
                {
                    Console.WriteLine("Client chose not to continue.");
                }
            }
            else
            {
                const string skipMessage = "skip";
                var skipMessageBytes = Encoding.UTF8.GetBytes(skipMessage + "\r\n");
                stream.Write(skipMessageBytes, 0, skipMessageBytes.Length);
                Console.WriteLine("Sent 'skip' message to client.");
            }

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