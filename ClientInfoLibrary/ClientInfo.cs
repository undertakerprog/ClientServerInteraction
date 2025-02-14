namespace ClientInfoLibrary
{
    public class ClientInfo()
    {
        public string IpAddress { get; set;  } = "Unknown";
        public string FileName { get; set; } = "No File";
        public long DownloadedBytes { get; set; } = 0;
        public DateTime LastActivity { get; set; } = DateTime.MinValue;
        public bool CanResumeDownload { get; set; } = true;

        public void UpdateActivity(string ipAddress, string fileName, long downloadedBytes)
        {
            IpAddress = ipAddress;
            FileName = fileName;
            DownloadedBytes = downloadedBytes;
            LastActivity = DateTime.Now;
            CanResumeDownload = true;
        }

        public void MarkInactive()
        {
            CanResumeDownload = false;
        }

        public void MarkActive()
        {
            CanResumeDownload = true;
        }
    }
}