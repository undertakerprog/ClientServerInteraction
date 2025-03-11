﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using ClientInfoLibrary;

namespace Server
{
    public static class CommandProcessor
    {
        private static readonly string ServerDirectory = Path.Combine(Environment.CurrentDirectory, "ServerFiles");
        private const int PacketSize = 2048;

        public static string ProcessCommand(string command, IPEndPoint clientEndPoint, UdpClient udpClient)
        {
            var parts = command.Split(' ', 2);
            var mainCommand = parts[0].ToUpper();
            var argument = parts.Length > 1 ? parts[1] : string.Empty;

            return mainCommand switch
            {
                "ECHO" => $"ECHO: {argument}\r\n",
                "TIME" => $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n",
                "LIST" => GetFileList(ServerDirectory),
                "DOWNLOAD" => UdpDownloadFile(argument, clientEndPoint, udpClient),
                //"UPLOAD" => UdpUploadFile
                "CLOSE" or "EXIT" or "QUIT" => "Connection closed\r\n",
                _ => "Unknown command\r\n"
            };
        }

        public static string ProcessCommand(string command, TcpClient client, ClientManager clientManager)
        {
            var stream = client.GetStream();
            var parts = command.Split(' ', 3);
            var mainCommand = parts[0].ToUpper();
            var argument = parts.Length > 1 ? parts[1] : string.Empty;


            var ipAddress = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();

            return mainCommand switch
            {
                "ECHO" => $"ECHO: {argument}\r\n",
                "TIME" => $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n",
                "LIST" => GetFileList(ServerDirectory),
                "UPLOAD" => TcpUploadFile(argument, stream),
                "DOWNLOAD" => TcpDownloadFile(argument, stream, clientManager, ipAddress),
                "CLOSE" or "EXIT" or "QUIT" => "Connection closed\r\n",
                _ => "Unknown command\r\n"
            };
        }

        private static string TcpDownloadFile(string fileName, NetworkStream stream, ClientManager clientManager, string ipAddress)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return "Error: File name not specified.\r\n";

                var filePath = Path.Combine(ServerDirectory, fileName);
                if (!File.Exists(filePath))
                    return "Error: File not found.\r\n";

                clientManager.AddOrUpdateClient(ipAddress, fileName);

                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;
                var fileSizeBytes = BitConverter.GetBytes(fileSize);
                var startFlagBytes = "BEG!"u8.ToArray();
                var endFlagBytes = "END!"u8.ToArray();

                stream.Write(fileSizeBytes, 0, fileSizeBytes.Length);
                stream.Write(startFlagBytes, 0, startFlagBytes.Length);
                Console.WriteLine($"[SERVER] Sent file size: {fileSize} bytes");

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[PacketSize];
                int bytesRead;

                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }

                stream.Write(endFlagBytes, 0, endFlagBytes.Length);
                Console.WriteLine("[SERVER] Transmission end flag sent");

                clientManager.ClearClientData(ipAddress);
                return $"File {fileName} downloaded successfully.\r\n";
            }
            catch
            {
                return "Error sending file.\r\n";
            }
        }

        private static string UdpDownloadFile(string fileName, IPEndPoint clientEndPoint, UdpClient udpClient)
        {
            var filePath = Path.Combine(ServerDirectory, fileName);

            if (!File.Exists(filePath))
            {
                return "Error: File not found\r\n";
            }

            var fileBytes = File.ReadAllBytes(filePath);
            var totalPackets = (int)Math.Ceiling((double)fileBytes.Length / PacketSize);

            udpClient.Send(Encoding.UTF8.GetBytes($"START {fileName} {fileBytes.Length} {totalPackets}"), clientEndPoint);

            var sentPackets = new HashSet<int>();
            if (sentPackets == null) throw new ArgumentNullException(nameof(sentPackets));

            for (var i = 0; i < totalPackets; i++)
            {
                SendPacket(i, fileBytes, clientEndPoint, udpClient);
                sentPackets.Add(i);
            }

            udpClient.Client.ReceiveTimeout = 2000;
            try
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var buffer = udpClient.Receive(ref remoteEndPoint);
                var lostPackets = Encoding.UTF8.GetString(buffer).Split(',').Select(int.Parse).ToList();

                foreach (var packetNum in lostPackets)
                {
                    SendPacket(packetNum, fileBytes, clientEndPoint, udpClient);
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("No lost packets reported.");
            }

            udpClient.Send("END"u8, clientEndPoint);
            return "File transfer complete\r\n";
        }

        private static void SendPacket(int packetNum, byte[] fileBytes, IPEndPoint clientEndPoint, UdpClient udpClient)
        {
            var offset = packetNum * PacketSize;
            var remaining = Math.Min(PacketSize, fileBytes.Length - offset);
            var packet = new byte[remaining + 4];

            BitConverter.GetBytes(packetNum).CopyTo(packet, 0);
            Array.Copy(fileBytes, offset, packet, 4, remaining);

            udpClient.Send(packet, packet.Length, clientEndPoint);
        }

        private static string TcpUploadFile(string fileName, NetworkStream stream)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return "Error: No file name provided.\r\n";
                }

                if (!Directory.Exists(ServerDirectory))
                {
                    Directory.CreateDirectory(ServerDirectory);
                }

                var filePath = Path.Combine(ServerDirectory, fileName);

                var buffer = new byte[8];
                stream.ReadExactly(buffer, 0, buffer.Length);
                var fileSize = BitConverter.ToInt64(buffer, 0);

                Console.WriteLine($"Receiving file: {fileName} ({fileSize} bytes)");

                using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                var receivedBytes = 0L;
                var fileBuffer = new byte[PacketSize];
                var startTime = DateTime.Now;

                while (receivedBytes < fileSize)
                {
                    var bytesRead = stream.Read(fileBuffer, 0, fileBuffer.Length);
                    fileStream.Write(fileBuffer, 0, bytesRead);
                    receivedBytes += bytesRead;
                }

                var elapsedTime = DateTime.Now - startTime;
                var bitRate = receivedBytes / elapsedTime.TotalSeconds;

                Console.WriteLine($"File received successfully: {fileName}");

                return $"File {fileName} uploaded successfully. Bit-rate: {bitRate:F2} bytes/second\r\n";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving file: {ex.Message}");
                return "Error during file upload.\r\n";
            }
        }

        private static string GetFileList(string serverDirectory)
        {
            try
            {
                if (!Directory.Exists(serverDirectory))
                {
                    Directory.CreateDirectory(serverDirectory);
                }
                var files = Directory.GetFiles(serverDirectory);
                if (files.Length == 0)
                {
                    return "No files found in the server directory.";
                }

                var fileList = string.Join("\n", files.Select(Path.GetFileName));
                return $"Files on server:\n{fileList}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing or creating directory: {ex.Message}");
                return "Error: Could not access or create server directory.\r\n";
            }
        }
    }
}