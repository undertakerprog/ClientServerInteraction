namespace ClientInfoLibrary
{
    public class ClientInfo()
    {
        public string IpAddress { get; set;  } = "Unknown";
        public string FileName { get; set; } = "No File";
        public DateTime LastActivity { get; set; } = DateTime.MinValue;
        public bool CanResumeDownload { get; set; } = true;

        public void UpdateActivity(string ipAddress, string fileName)
        {
            IpAddress = ipAddress;
            FileName = fileName;
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