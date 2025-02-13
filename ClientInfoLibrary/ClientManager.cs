using System.Collections.Concurrent;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace ClientInfoLibrary
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
        private readonly ConcurrentDictionary<string, Timer> _timers = new();
        private const string DataFile = "clients.json";

        public void AddOrUpdateClient(string ipAddress, string fileName, long downloadedBytes)
        {
            _clients.AddOrUpdate(ipAddress,
                _ => new ClientInfo(ipAddress, fileName, downloadedBytes),
                (_, existingClient) =>
                {
                    existingClient.UpdateActivity(downloadedBytes);
                    ResetTimer(ipAddress);
                    return existingClient;
                });
            ResetTimer(ipAddress);
        }

        public void SetClientActive(string ipAddress)
        {
            if (!_clients.TryGetValue(ipAddress, out var clientInfo)) return;
            clientInfo.MarkActive();
            StopTimer(ipAddress);
        }

        public void LoadClients()
        {
            if (!File.Exists(DataFile))
                return;

            try
            {
                var json = File.ReadAllText(DataFile);
                var clients = JsonSerializer.Deserialize<ConcurrentDictionary<string, ClientInfo>>(json);
                if (clients == null)
                    return;
                foreach (var kvp in clients)
                {
                    _clients[kvp.Key] = kvp.Value;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving clients: {ex.Message}");
            }
        }

        private void StopTimer(string ipAddress)
        {
            if (!_timers.TryGetValue(ipAddress, out var timer)) return;
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
                var timer = new Timer(10000) { AutoReset = false };
                timer.Elapsed += (_, _) => SetClientInactive(ipAddress);
                timer.Start();
                _timers[ipAddress] = timer;
            }
        }

        private void SetClientInactive(string ipAddress)
        {
            if (!_clients.TryGetValue(ipAddress, out var clientInfo))
                return;

            clientInfo.MarkInactive();
            Console.WriteLine($"The download timeout for the client({ipAddress}) has expired");

            SaveClients();
        }

        private void SaveClients()
        {
            try
            {
                var json = JsonSerializer.Serialize(_clients);
                File.WriteAllText(DataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving clients: {ex.Message}");
            }
        }
    }
}
