using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class Commands
    {
        private const int BufferSize = 1024;
        private static readonly string ClientDirectory = Path.Combine(AppContext.BaseDirectory, "ClientFiles");
        private static readonly object Lock = new();

        public static void ProcessCommand(string command, NetworkStream stream)
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
                        SendCommand(stream, command);
                        var response = ReceiveResponse(stream);
                        Console.WriteLine($"Server response: {response}");
                        break;

                    case "UPLOAD":
                        if (string.IsNullOrWhiteSpace(argument))
                        {
                            Console.WriteLine("Error: File name must be specified for UPLOAD command.");
                        }
                        else
                        {
                            UploadFile(argument, stream);
                            response = ReceiveResponse(stream);
                            Console.WriteLine($"Server response: {response}");
                        }
                        break;

                    case "DOWNLOAD":
                        if (string.IsNullOrEmpty(argument))
                        {
                            Console.WriteLine("Error: File name must be specified for DOWNLOAD command");
                        }
                        else
                        {
                            DownloadFile(argument, stream);
                            var serverResponse = ReceiveResponse(stream);
                            Console.WriteLine($"Server response: {serverResponse}");
                        }
                        break;

                    default:
                        Console.WriteLine("Unknown command. Available commands: ECHO <message>, TIME, CLOSE/QUIT/EXIT.");
                        break;
                }
            }
        }

        private static void DownloadFile(string fileName, NetworkStream stream)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Console.WriteLine("Error: No file name provided.");
                    return;
                }

                // Убедимся, что папка `ClientDirectory` существует
                if (!Directory.Exists(ClientDirectory))
                {
                    Directory.CreateDirectory(ClientDirectory);
                }

                var command = $"DOWNLOAD {fileName}\r\n";
                var commandBytes = Encoding.UTF8.GetBytes(command);
                stream.Write(commandBytes, 0, commandBytes.Length);
                stream.Flush();

                var sizeBuffer = new byte[8];
                if (!ReadExactly(stream, sizeBuffer, 8))
                {
                    Console.WriteLine("Error: Failed to read file size.");
                    return;
                }

                var fileSize = BitConverter.ToInt64(sizeBuffer, 0);
                if (fileSize <= 0)
                {
                    Console.WriteLine("Error: File not found or empty.");
                    return;
                }

                Console.WriteLine($"Receiving file: {fileName} ({fileSize} bytes)");

                var savePath = Path.Combine(ClientDirectory, fileName);
                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
                var receivedBytes = 0L;
                var buffer = new byte[2048];

                while (receivedBytes < fileSize)
                {
                    var bytesRead = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, fileSize - receivedBytes));
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Error: Connection lost during download.");
                        return;
                    }

                    fileStream.Write(buffer, 0, bytesRead);
                    receivedBytes += bytesRead;
                }

                Console.WriteLine($"Downloading file: {fileName} ({fileSize} bytes)");
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

        private static void SendCommand(NetworkStream stream, string command)
        {
            var commandBytes = Encoding.UTF8.GetBytes(command + "\r\n");
            stream.Write(commandBytes, 0, commandBytes.Length);
        }

        private static string ReceiveResponse(NetworkStream stream)
        {
            var buffer = new byte[BufferSize];
            var response = string.Empty;

            try
            {
                Thread.Sleep(50);
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving response: {ex.Message}");
            }

            return response;
        }

    }
}