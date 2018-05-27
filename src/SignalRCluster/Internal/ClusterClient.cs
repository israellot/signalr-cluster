using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public class ClusterClient
    {

        ClusterManager _clusterManager;

        public ClusterClient(ClusterManager clusterManager)
        {
            _clusterManager = clusterManager;
        }

        public Task BroadcastDirectoryOperation(IEnumerable<RemoteDirectoryOperation> operations)
        {
            if (operations.Count() == 0)
                return Task.CompletedTask;

            var tasks = new List<Task>();

            foreach(var client in _clusterManager.GetAllClients())
            {
                if (client.IsConnected)
                {
                    var task= client.Connection.InvokeAsync("DirectoryOperations", operations);
                    tasks.Add(task);
                }
            }

            return Task.WhenAll(tasks);
        }

        public async Task SendToAll(string[] nodes, string hubName, string methodName, object[] args, string[] excludeConnectionIds = null)
        {

            var tasks = new List<Task>();
            foreach (var node in nodes)
            {
                var nodeProxy = _clusterManager.GetClient(node);

                if (nodeProxy != null && nodeProxy.IsConnected)
                {
                    var t = nodeProxy.Connection.InvokeAsync("SendToAllConnections", hubName, methodName, args, excludeConnectionIds);
                    tasks.Add(t);
                }

            }

            await Task.WhenAll(tasks);
        }

    }
}
