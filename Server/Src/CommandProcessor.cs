using System.Net.Sockets;
using ClientInfoLibrary;

namespace Server
{
    public static class CommandProcessor
    {
        private static readonly ClientManager ClientManager = new();
        private static readonly string ServerDirectory = Path.Combine(Environment.CurrentDirectory, "ServerFiles");

        public static string ProcessCommand(string command, TcpClient client)
        {
            var stream = client.GetStream();
            var parts = command.Split(' ', 3);
            var mainCommand = parts[0].ToUpper();
            var argument = parts.Length > 1 ? parts[1] : string.Empty;
            var startByte = parts.Length > 2 && long.TryParse(parts[2], out var parsedStartByte) ? parsedStartByte : 0;

            var ipAddress = ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            return mainCommand switch
            {
                "ECHO" => $"ECHO: {argument}\r\n",
                "TIME" => $"Server time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n",
                "LIST" => GetFileList(ServerDirectory),
                "UPLOAD" => UploadFile(argument, stream),
                "DOWNLOAD" => DownloadFile(argument, stream, ipAddress, startByte),
                "CLOSE" or "EXIT" or "QUIT" => "Connection closed\r\n",
                _ => "Unknown command\r\n"
            };
        }

        private static string DownloadFile(string fileName, NetworkStream stream, string ipAddress, long startByte = 0)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                    return "Error: No file name provided.\r\n";

                var filePath = Path.Combine(ServerDirectory, fileName);
                if (!File.Exists(filePath))
                    return "Error: File not found.\r\n";

                var fileInfo = new FileInfo(filePath);
                var fileSize = fileInfo.Length;

                ClientManager.AddOrUpdateClient(ipAddress, fileName, startByte);

                var sizeBytes = BitConverter.GetBytes(fileSize);
                stream.Write(sizeBytes, 0, sizeBytes.Length);

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                fileStream.Seek(startByte, SeekOrigin.Begin);

                var buffer = new byte[1024];
                var totalBytesSent = startByte;
                var startTime = DateTime.Now;

                while (totalBytesSent < fileSize)
                {
                    var bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    stream.Write(buffer, 0, bytesRead);
                    totalBytesSent += bytesRead;

                    ClientManager.AddOrUpdateClient(ipAddress, fileName, totalBytesSent);
                }

                return $"File {fileName} downloaded successfully.\r\n";
            }
            catch (Exception ex)
            {
                return $"Error during file download: {ex.Message}\r\n";
            }
        }

        private static string UploadFile(string fileName, NetworkStream stream)
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
                var fileBuffer = new byte[1024];
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
                Console.WriteLine($"Bit-rate: {bitRate:F2} bytes/second");

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