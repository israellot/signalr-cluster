using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public class ClusterHub : Hub<IClusterHub>
    {

        ClusterManager _clusterManager;
        IClusterSignalRMembershipProvider _membershipProvider;
        ClusterLocalNodeDirectory _localDirectory;
        ClusterRemoteDirectory _remoteDirectory;
        ClusterHubLifetimeManagerCache _lifetimeManagerCache;
        IServiceProvider _serviceProvider;

        public ClusterHub(
            IClusterSignalRMembershipProvider membershipProvider,
            ClusterManager clusterManager,
            IServiceProvider serviceProvider,
            ClusterLocalNodeDirectory localDirectory,
            ClusterRemoteDirectory remoteDirectory,
            ClusterHubLifetimeManagerCache lifetimeManagerCache
            )
        {
            _clusterManager = clusterManager;
            _membershipProvider = membershipProvider;
            _localDirectory = localDirectory;
            _remoteDirectory = remoteDirectory;
            _serviceProvider = serviceProvider;
            _lifetimeManagerCache = lifetimeManagerCache;
        }

        public override Task OnConnectedAsync()
        {
            var headers = this.Context.GetHttpContext().Request.Headers;

            if (headers.TryGetValue("X-SignalR-Cluster-Node", out var nodeName))
            {
                if (headers.TryGetValue("X-SignalR-Cluster-Node-Id", out var nodeUniqueId))
                {
                    Context.Items["Cluster-Node-Id"] = nodeUniqueId;
                    Context.Items["Cluster-Node"] = nodeName;

                }
                else
                {
                    // TODO
                }
            }


            return Task.CompletedTask;

        }

        

        public Task DirectoryOperations(RemoteDirectoryOperation[] ops)
        {
            _remoteDirectory.PerformActions(ops);

            return Task.CompletedTask;
        }

        public Task SendToAllConnections(string hubName, string methodName, object[] args, string[] excludeConnectionIds = null)
        {            
            var hubLifetimeManager = _lifetimeManagerCache.Get(hubName);

            hubLifetimeManager?.SendToAllLocalConnection(methodName, args, excludeConnectionIds);

            return Task.CompletedTask;
        }

        public Task<string> GetUniqueId()
        {
            return Task.FromResult(_membershipProvider.GetOwnUniqueId());
        }

    }

    
}
