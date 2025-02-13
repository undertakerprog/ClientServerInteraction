namespace ClientInfoLibrary
{
    public class ClientInfo(string ipAddress, string fileName, long downloadedBytes)
    {
        public string IpAddress { get; set;  } = ipAddress;
        public string FileName { get; set; } = fileName;
        public long DownloadedBytes { get; set; } = downloadedBytes;
        public DateTime LastActivity { get; private set; } = DateTime.Now;
        public bool CanResumeDownload { get; set; }

        public void UpdateActivity(string ipAddress, string fileName, long downloadedBytes)
        {
            IpAddress = ipAddress;
            FileName = fileName;
            DownloadedBytes = downloadedBytes;
            LastActivity = DateTime.Now;
            CanResumeDownload = false;
        }

        public void MarkInactive()
        {
            CanResumeDownload = true;
        }

        public void MarkActive()
        {
            CanResumeDownload = false;
        }
    }
}