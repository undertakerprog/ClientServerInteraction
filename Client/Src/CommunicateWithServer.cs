using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class CommunicateWithServer
    {
        public static void Communicate(NetworkStream stream)
        {
            ReceiveAndRespondToServerPrompt(stream);

            while (true)
            {
                Console.Write("Commands:\n" +
                              "1.ECHO <message>\n" +
                              "2.TIME\n" +
                              "3.LIST\n" +
                              "4.UPLOAD <file/filepath>\n" +
                              "5.DOWNLOAD <file>\n" +
                              "6.CLOSE/EXIT/QUIT\nEnter command: ");
                var command = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(command))
                    continue;

                try
                {
                    Commands.ProcessCommand(command, stream);

                    if (!Commands.IsExitCommand(command)) continue;
                    Console.WriteLine("Closing connection...");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static bool ReceiveAndRespondToServerPrompt(NetworkStream stream)
        {
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (message == "skip")
            {
                return true;
            }

            Console.WriteLine($"File available for download: {message}");
            Console.Write("Enter your response (yes/no): ");
            var response = Console.ReadLine()?.Trim().ToLower() ?? "no";

            var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
            stream.Write(responseBytes, 0, responseBytes.Length);

            return response == "yes";
        }
    }
}
