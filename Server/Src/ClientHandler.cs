﻿using System.Net.Sockets;
using System.Text;
using ClientInfoLibrary;

namespace Server
{
    public class ClientHandler(TcpClient client)
    {
        public static readonly ClientManager ClientManager = new();

        public void HandleClient(string ipAddress)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            Console.WriteLine(ClientManager.CanDownloadFile(ipAddress));
            Console.WriteLine(ClientManager.GetFileName(ipAddress));

            if (ClientManager.CanDownloadFile(ipAddress) && ClientManager.GetFileName(ipAddress) != "No File")
            {
                if (!AskClientToContinue(stream))
                {
                    Console.WriteLine("Client declined to continue. Connection closed.");
                }
            }
            else
            {
                var skipMessage = Encoding.UTF8.GetBytes("skip\r\n");
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
                    Console.WriteLine($"Received command: {request}");

                    var response = CommandProcessor.ProcessCommand(request, client, ClientManager);
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

        private static bool AskClientToContinue(NetworkStream stream)
        {
            const string message = "Do you want to continue? (yes/no)\r\n";
            var messageBytes = Encoding.UTF8.GetBytes(message);
            stream.Write(messageBytes, 0, messageBytes.Length);

            var buffer = new byte[1024];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim().ToLower();

            return response == "yes";
        }
    }
}