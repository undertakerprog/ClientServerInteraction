using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class CommunicateWithServer
    {
        public static void Communicate(object connection, IPEndPoint serverEndPoint = null!)
        {
            switch (connection)
            {
                case NetworkStream tcpStream:
                    ReceiveAndRespondToServerPrompt(tcpStream);
                    HandleCommands(
                        command => Commands.ProcessCommand(command, tcpStream, ref serverEndPoint),
                        Commands.IsExitCommand
                    );
                    break;

                case UdpClient udpClient:
                    HandleCommands(
                        command => Commands.ProcessCommand(command, udpClient, ref serverEndPoint),
                        Commands.IsExitCommand
                    );
                    break;

                default:
                    throw new ArgumentException("Invalid connection type");
            }
        }

        private static void HandleCommands(Action<string> processCommand, Func<string, bool> isExitCommand)
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
                    processCommand(command);

                    if (!isExitCommand(command))
                        continue;
                    Console.WriteLine("Closing connection...");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static void ReceiveAndRespondToServerPrompt(NetworkStream stream)
        {
            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            var message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (!message.StartsWith("RESUME ")) return;
            var fileName = message[7..].Trim();
            Console.WriteLine($"An incomplete download was detected for file: {fileName}. Continue downloading...");

            Commands.TcpDownloadFile(fileName, stream);
        }
    }
}