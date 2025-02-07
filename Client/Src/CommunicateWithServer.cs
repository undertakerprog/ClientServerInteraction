using System.Net.Sockets;

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
    }
}
