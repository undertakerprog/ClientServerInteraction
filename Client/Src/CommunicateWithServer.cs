using ClientInfoLibrary;
using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class CommunicateWithServer
    {
        public static void Communicate(NetworkStream stream)
        {
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

        public static void CheckResumableFiles(NetworkStream stream)
        {
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (message == "skip")
            {
                return;
            }

            Console.WriteLine(message);

            Console.Write("Enter answer (yes/no): ");
            var answer = Console.ReadLine()?.Trim().ToLower();

            var responseBytes = Encoding.UTF8.GetBytes(answer + "\r\n");
            stream.Write(responseBytes, 0, responseBytes.Length);

            if (answer == "yes")
            {
                Console.WriteLine("Continue working...");
            }
            else
            {
                Console.WriteLine("Don't continue working...");
            }
        }

    }
}
