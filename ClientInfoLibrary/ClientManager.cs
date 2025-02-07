using System.Collections.Concurrent;

namespace ClientInfoLibrary
{
    public class ClientManager
    {
        private readonly ConcurrentDictionary<string, ClientInfo> _clients = new();
        private readonly Timer _cleanupTimer;

        public ClientManager()
        {
            _cleanupTimer = new Timer(CleanupInactiveClients, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        public void AddOrUpdateClient(string ipAddress, string fileName, long downloadedBytes)
        {
            _clients.AddOrUpdate(ipAddress,
                _ => new ClientInfo(ipAddress, fileName, downloadedBytes),
                (_, existingClient) =>
                {
                    existingClient.UpdateActivity(downloadedBytes);
                    return existingClient;
                });
        }

        public ClientInfo? GetClientInfo(string ipAddress)
        {
            return _clients.GetValueOrDefault(ipAddress);
        }

        private void CleanupInactiveClients(object? state)
        {
            var cutoff = DateTime.Now - TimeSpan.FromMinutes(2);
            var inactiveClients = _clients.Where(pair => pair.Value.LastActivity < cutoff).Select(pair => pair.Key)
                .ToList();

            foreach (var clientIp in inactiveClients)
            {
                _clients.TryRemove(clientIp, out _);
            }
        }
    }
}
