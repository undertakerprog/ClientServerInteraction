using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Src
{
    public class UdpClientHandler(IPEndPoint clientEndPoint, UdpClient udpClient)
    {
        public static void HandleMessage(string message, string ipAddress, IPEndPoint clientEndPoint, UdpClient udpClient)
        {
            if (message.StartsWith("CONNECTED"))
            {
                Console.WriteLine($"[UDP] Client connected from {ipAddress}");
            }
            else
            {
                Console.WriteLine($"[UDP] Received message from {ipAddress}: {message}");

                var response = CommandProcessor.ProcessCommand(message, clientEndPoint, udpClient);
                var responseBytes = Encoding.UTF8.GetBytes(response + "\r\n");
                udpClient.Send(responseBytes, responseBytes.Length, clientEndPoint);
            }
        }
    }
}