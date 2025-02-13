using System.Net.Sockets;
using System.Text;

namespace Client.Src
{
    public class Commands
    {
        private const int BufferSize = 1024;
        private static readonly string ClientDirectory = Path.Combine(AppContext.BaseDirectory, "ClientFiles");

        public static void ProcessCommand(string command, NetworkStream stream)
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
                    }
                    break;

                default:
                    Console.WriteLine("Unknown command. Available commands: ECHO <message>, TIME, CLOSE/QUIT/EXIT.");
                    break;
            }
        }

        public static void DownloadFile(string fileName, NetworkStream stream)
        {
            var filePath = Path.Combine(ClientDirectory, fileName);
            var startByte = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

            var command = $"DOWNLOAD {fileName} {startByte / 1024}Kb\r\n";
            var commandBytes = Encoding.UTF8.GetBytes(command);
            stream.Write(commandBytes, 0, commandBytes.Length);

            var buffer = new byte[8];
            int bytesRead = 0, totalBytesRead = 0;

            while (totalBytesRead < 8)
            {
                bytesRead = stream.Read(buffer, totalBytesRead, 8 - totalBytesRead);
                if (bytesRead == 0)
                    throw new Exception("Connection closed unexpectedly.");
                totalBytesRead += bytesRead;
            }

            var fileSize = BitConverter.ToInt64(buffer, 0);
            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);

            var fileBuffer = new byte[1024];
            var totalBytesReceived = startByte;

            while (totalBytesReceived < fileSize)
            {
                bytesRead = stream.Read(fileBuffer, 0, fileBuffer.Length);
                if (bytesRead == 0)
                    break;

                fileStream.Write(fileBuffer, 0, bytesRead);
                totalBytesReceived += bytesRead;
            }

            Console.WriteLine($"File downloaded successfully: {fileName}");

            var confirmationMessage = $"FILE_RECEIVED {fileName}\r\n";
            var confirmationBytes = Encoding.UTF8.GetBytes(confirmationMessage);
            stream.Write(confirmationBytes, 0, confirmationBytes.Length);
        }


        //public static void DownloadFile(string fileName, NetworkStream stream)
        //{
        //    var filePath = Path.Combine(ClientDirectory, fileName);

        //    if (File.Exists(filePath))
        //    {
        //        Console.Write($"File {fileName} already exists. Do you want to overwrite it? (y/n): ");
        //        var userInput = Console.ReadLine()?.Trim().ToLower();

        //        if (userInput != "y")
        //        {
        //            Console.WriteLine("File download cancelled.");
        //            return;
        //        }

        //        try
        //        {
        //            File.Delete(filePath);
        //            Console.WriteLine($"Old file {fileName} deleted.");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error deleting old file {fileName}: {ex.Message}");
        //            return;
        //        }
        //    }

        //    var startByte = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

        //    var command = $"DOWNLOAD {fileName} {startByte / 1024}Kb\r\n";
        //    var commandBytes = Encoding.UTF8.GetBytes(command);
        //    stream.Write(commandBytes, 0, commandBytes.Length);

        //    var buffer = new byte[8];
        //    int bytesRead = 0, totalBytesRead = 0;

        //    while (totalBytesRead < 8)
        //    {
        //        bytesRead = stream.Read(buffer, totalBytesRead, 8 - totalBytesRead);
        //        if (bytesRead == 0)
        //            throw new Exception("Connection closed unexpectedly.");
        //        totalBytesRead += bytesRead;
        //    }

        //    var fileSize = BitConverter.ToInt64(buffer, 0);
        //    using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);

        //    var fileBuffer = new byte[1024];
        //    var totalBytesReceived = startByte;

        //    while (totalBytesReceived < fileSize)
        //    {
        //        bytesRead = stream.Read(fileBuffer, 0, fileBuffer.Length);
        //        if (bytesRead == 0)
        //            break;

        //        fileStream.Write(fileBuffer, 0, bytesRead);
        //        totalBytesReceived += bytesRead;
        //    }

        //    Console.WriteLine($"File downloaded successfully: {fileName}");
        //}

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
                var totalBytesSent = 0L;
                var startTime = DateTime.Now;

                int bytesRead;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, bytesRead);
                    totalBytesSent += bytesRead;
                }

                var elapsedTime = DateTime.Now - startTime;
                var bitRate = totalBytesSent / elapsedTime.TotalSeconds;

                Console.WriteLine($"File uploaded successfully. Bit-rate: {bitRate:F2} bytes/second");
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
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
        }
    }
}