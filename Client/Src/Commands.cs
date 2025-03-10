﻿using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class Commands
    {
        private const int BufferSize = 1024;
        private static readonly string ClientDirectory = Path.Combine(AppContext.BaseDirectory, "ClientFiles");
        private static readonly object Lock = new();

        public static void ProcessCommand(string command, object connection, ref IPEndPoint serverEndPoint)
        {
            lock (Lock)
            {
                var parts = command.Split(' ', 2);
                var mainCommand = parts[0].ToUpper();
                var argument = parts.Length > 1 ? parts[1] : string.Empty;

                switch (mainCommand)
                {
                    case "ECHO":
                    case "TIME":
                    case "LIST":
                    case "CLOSE":
                    case "QUIT":
                    case "EXIT":
                        SendCommand(connection, command, serverEndPoint);
                        var response = ReceiveResponse(connection, ref serverEndPoint);
                        Console.WriteLine($"Server response: {response}");
                        break;

                    case "UPLOAD":
                        if (string.IsNullOrWhiteSpace(argument))
                        {
                            Console.WriteLine("Error: File name must be specified for UPLOAD command.");
                        }
                        else
                        {
                            if (connection is NetworkStream stream)
                                UploadFile(argument, stream);
                            //else if (connection is UdpClient udpClient)
                            //    UploadFile(argument, udpClient, serverEndPoint);
                            else
                                throw new ArgumentException("Invalid connection type");
                        }
                        break;

                    case "DOWNLOAD":
                        if (string.IsNullOrEmpty(argument))
                        {
                            Console.WriteLine("Error: File name must be specified for DOWNLOAD command");
                        }
                        else
                        {
                            if (connection is NetworkStream stream)
                                DownloadFile(argument, stream);
                            //else if (connection is UdpClient udpClient)
                            //    DownloadFile(argument, udpClient, serverEndPoint);
                            else
                                throw new ArgumentException("Invalid connection type");
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown command. Available commands: ECHO <message>, TIME, CLOSE/QUIT/EXIT.");
                        break;
                }
            }
        }

        public static void DownloadFile(string fileName, NetworkStream stream)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Error: File name not specified.");
                    return;
                }

                if (!Directory.Exists(ClientDirectory))
                {
                    Directory.CreateDirectory(ClientDirectory);
                }

                var command = $"DOWNLOAD {fileName}\r\n";
                var commandBytes = Encoding.UTF8.GetBytes(command);
                stream.Write(commandBytes, 0, commandBytes.Length);

                var sizeBuffer = new byte[8];
                if (!ReadExactly(stream, sizeBuffer, 8))
                {
                    Console.WriteLine("Error: Could not read file size.");
                    return;
                }

                var fileSize = BitConverter.ToInt64(sizeBuffer, 0);
                if (fileSize <= 0)
                {
                    Console.WriteLine("Error: File not found or empty.");
                    return;
                }

                var flagBuffer = new byte[4];
                if (!ReadExactly(stream, flagBuffer, 4) || Encoding.UTF8.GetString(flagBuffer) != "BEG!")
                {
                    Console.WriteLine("Error: Invalid start flag.");
                    return;
                }

                Console.WriteLine($"Beginning start downloading the file: {fileName} ({fileSize} bytes)");

                var savePath = Path.Combine(ClientDirectory, fileName);
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                var receivedBytes = 0L;
                var buffer = new byte[4096];

                while (receivedBytes < fileSize)
                {
                    var bytesRead = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - receivedBytes));
                    if (bytesRead <= 0)
                    {
                        Console.WriteLine("Error: Connection lost.");
                        return;
                    }

                    fileStream.Write(buffer, 0, bytesRead);
                    receivedBytes += bytesRead;
                }

                if (!ReadExactly(stream, flagBuffer, 4) || Encoding.UTF8.GetString(flagBuffer) != "END!")
                {
                    Console.WriteLine("Error: Invalid transfer end flag.");
                    return;
                }

                Console.WriteLine($"File {fileName} downloaded successfully ({fileSize} bytes).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
            }
        }
        
        private static bool ReadExactly(NetworkStream stream, byte[] buffer, int count)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var bytesRead = stream.Read(buffer, totalRead, count - totalRead);
                if (bytesRead == 0)
                    return false;

                totalRead += bytesRead;
            }
            return true;
        }


        private static void UploadFile(string filePath, NetworkStream stream)
        {
            if (!Path.IsPathRooted(filePath))
            {
                filePath = Path.Combine(ClientDirectory, filePath);
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            try
            {
                var fileName = Path.GetFileName(filePath);
                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;

                var command = $"UPLOAD {fileName}\r\n";
                var commandBytes = Encoding.UTF8.GetBytes(command);
                stream.Write(commandBytes, 0, commandBytes.Length);

                var sizeBytes = BitConverter.GetBytes(fileSize);
                stream.Write(sizeBytes, 0, sizeBytes.Length);

                Console.WriteLine($"Uploading file: {fileName} ({fileSize} bytes)");

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[1024];

                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file upload: {ex.Message}");
            }
        }

        public static bool IsExitCommand(string command)
        {
            var upperCommand = command.ToUpper();
            return upperCommand is "CLOSE" or "EXIT" or "QUIT";
        }

        private static void SendCommand(object connection, string command, IPEndPoint serverEndPoint = null)
        {
            var commandBytes = Encoding.UTF8.GetBytes(command + "\r\n");

            switch (connection)
            {
                case NetworkStream stream:
                    stream.Write(commandBytes, 0, commandBytes.Length);
                    break;

                case UdpClient udpClient:
                    udpClient.Send(commandBytes, commandBytes.Length, serverEndPoint);
                    break;

                default:
                    throw new ArgumentException("Invalid connection type");
            }
        }

        private static string ReceiveResponse(object connection, ref IPEndPoint serverEndPoint)
        {
            var response = string.Empty;

            try
            {
                switch (connection)
                {
                    case NetworkStream stream:
                        var buffer = new byte[BufferSize];
                        Thread.Sleep(50);
                        var bytesRead = stream.Read(buffer, 0, buffer.Length);
                        response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        break;

                    case UdpClient udpClient:
                        var responseBytes = udpClient.Receive(ref serverEndPoint);
                        response = Encoding.UTF8.GetString(responseBytes).TrimEnd('\r', '\n');
                        break;

                    default:
                        throw new ArgumentException("Invalid connection type");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }

            return response;
        }
    }
}