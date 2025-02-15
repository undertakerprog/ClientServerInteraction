using System.Collections.Concurrent;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace ClientInfoLibrary
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
        private readonly ConcurrentDictionary<string, Timer> _timers = new();
        private const int TimeOutClient = 90000;

        public void AddOrUpdateClient(string ipAddress, string fileName)
        {
            _clients.AddOrUpdate(ipAddress,
                _ => new ClientInfo { IpAddress = ipAddress, FileName = fileName },
                (_, existingClient) =>
                {
                    existingClient.UpdateActivity(ipAddress, fileName);
                    ResetTimer(ipAddress);
                    return existingClient;
                });
            ResetTimer(ipAddress);
        }

        public void SetClientActive(string ipAddress)
        {
            if (!_clients.TryGetValue(ipAddress, out var clientInfo))
                return;
            clientInfo.MarkActive();
            StopTimer(ipAddress);
        }

        public string GetFileName(string ipAddress)
        {
            return _clients.TryGetValue(ipAddress, out var clientInfo) ? clientInfo.FileName : "No File";
        }

        public bool CanDownloadFile(string ipAddress)
        {
            return _clients.TryGetValue(ipAddress, out var clientInfo) && clientInfo.CanResumeDownload;
        }

        private void StopTimer(string ipAddress)
        {
            if (!_timers.TryGetValue(ipAddress, out var timer))
                return;
            timer.Stop();
            _timers.TryRemove(ipAddress, out _);
        }

        private void ResetTimer(string ipAddress)
        {
            if (_timers.TryGetValue(ipAddress, out var existingTimer))
            {
                existingTimer.Stop();
                existingTimer.Start();
            }
            else
            {
                var timer = new Timer(TimeOutClient) { AutoReset = false };
                timer.Elapsed += (_, _) => SetClientInactive(ipAddress);
                timer.Start();
                _timers[ipAddress] = timer;
            }
        }

        private void SetClientInactive(string ipAddress)
        {
            if (!_clients.TryGetValue(ipAddress, out var clientInfo))
                return;

            Console.WriteLine($"Session for client({ipAddress}) is timeout");
            clientInfo.MarkInactive();

        }
    }
}
