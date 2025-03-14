using System.Net.Sockets;
using System.Text;
using ClientInfoLibrary;

namespace Server
{
    public abstract class TcpClientHandler()
    {
        private static readonly ClientManager ClientManager = new();

        public static void HandleClient(TcpClient client, string ipAddress)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
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
                            Console.WriteLine($"[TCP - {ipAddress}] Received command: {request}");

                            var response = CommandProcessor.ProcessCommand(request, client, ClientManager);
                            var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                            stream.Write(responseBytes, 0, responseBytes.Length);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error handling client {ipAddress}: {ex.Message}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {ipAddress}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"[TCP] Client {ipAddress} disconnected.");
            }
        }
    }
}