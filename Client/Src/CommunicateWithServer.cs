using System.Net.Sockets;

namespace Client.Src
{
    public class CommunicateWithServer
    {
        public static void Communicate(NetworkStream stream)
        {
            while (true)
            {
                Console.Write("Commands - ECHO <message>/TIME/LIST/UPLOAD <file/filepath>/DOWNLOAD <file>/CLOSE/EXIT/QUIT\nEnter command: ");
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
    }
}
