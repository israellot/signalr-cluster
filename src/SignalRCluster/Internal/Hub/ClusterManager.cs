using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public class ClusterManager : IDisposable
    {
        bool _isDisposed;

        ILogger _logger;
        IClusterSignalRMembershipProvider _membershipProvider;
        ClusterRemoteDirectory _remoteDirectory;
        ClusterPeerNodeClientFactory _peerClientFactory;

        ConcurrentDictionary<string, ClusterPeerNodeClient> _peers = new ConcurrentDictionary<string, ClusterPeerNodeClient>();

        public ClusterManager(
            ILogger<ClusterManager> logger,
            ClusterRemoteDirectory remoteDirectory,
            ClusterPeerNodeClientFactory peerClientFactory,
            IClusterSignalRMembershipProvider membershipProvider)
        {
            _logger = logger;
            _membershipProvider = membershipProvider;
            _remoteDirectory = remoteDirectory;
            _peerClientFactory = peerClientFactory;

            var ignore = MonitorTask();
        }



     

       

        public ClusterPeerNodeClient GetClient(string nodeGuid)
        {
            return _peers.Values.FirstOrDefault(c => c.UniqueId == nodeGuid);
        }

        public IEnumerable<ClusterPeerNodeClient> GetAllClients()
        {
            return _peers.Values.ToArray();
        }

        private async Task MonitorTask()
        {
            while (!_isDisposed)
            {
                try
                {
                    var members = await _membershipProvider.GetClusterMembersAsync();

                    foreach (var m in members)
                    {
                        var node = _peers.GetOrAdd(m.Name, _ => {
                            _logger.LogInformation($"Adding peer {m.Name} on {m.Address}");
                            return _peerClientFactory.GetNewClient(m.Name, m.Address);
                        });

                    }

                    await Task.Delay(5000);
                }
                catch { }
            }
        }
                
        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
