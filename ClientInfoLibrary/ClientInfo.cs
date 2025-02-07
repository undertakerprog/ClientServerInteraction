namespace ClientInfoLibrary
{
    public class ClientInfo(string ipAddress, string fileName, long downloadedBytes)
    {
        public string IpAddress { get; } = ipAddress;
        public string FileName { get; set; } = fileName;
        public long DownloadedBytes { get; set; } = downloadedBytes;
        public DateTime LastActivity { get; private set; } = DateTime.Now;

        public void UpdateActivity(long downloadedBytes)
        {
            DownloadedBytes = downloadedBytes;
            LastActivity = DateTime.Now;
        }
    }
}
