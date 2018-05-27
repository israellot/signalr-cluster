using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{

    public class ClusterLocalNodeDirectory
    {

        public int Sequence { get; private set; }

        private readonly ConcurrentDictionary<string,ClusterNodeHubDirectory> _hubs = new ConcurrentDictionary<string,ClusterNodeHubDirectory>();

        private readonly ClusterNodeHubDirectoryHistory _actionHistory = new ClusterNodeHubDirectoryHistory();

        private readonly IClusterSignalRMembershipProvider _membershipProvider;

        private int lastBroadcastedSequence;

        private object _syncObject = new object();

        ClusterClient _clusterClient;

        public ClusterLocalNodeDirectory(
            ClusterClient clusterClient,
            IClusterSignalRMembershipProvider membershipProvider
            )
        {
            _clusterClient = clusterClient;
            _membershipProvider = membershipProvider;
        }

        public Task PerformAction(DirectoryAction action)
        {
            var _hub = _hubs.GetOrAdd(action.Hub, _ => new ClusterNodeHubDirectory());


            lock (_syncObject)
            {
                var operationSequence = this.Sequence;

                if (_hub.PerformOperation(action))
                {
                    var remoteOperation = new RemoteDirectoryOperation(_membershipProvider.GetOwnUniqueId(), Sequence, action);

                    this.Sequence = this.Sequence == Int32.MaxValue ? 0 : this.Sequence++;

                    _actionHistory.Add(action);

                    return _clusterClient.BroadcastDirectoryOperation(new[] { remoteOperation });
                }
               
            }

            return Task.CompletedTask;
            
        }

        public Task PerformActions(IEnumerable<DirectoryAction> actions)
        {

            var remoteOperations = new List<RemoteDirectoryOperation>();

            foreach(var g in actions.GroupBy(a => a.Hub))
            {
                var _hub = _hubs.GetOrAdd(g.Key, _ => new ClusterNodeHubDirectory());

                lock (_syncObject)
                {
                    foreach(var action in g)
                    {                        
                        if (_hub.PerformOperation(action))
                        {                            
                            remoteOperations.Add(new RemoteDirectoryOperation(_membershipProvider.GetOwnUniqueId(), Sequence, action));

                            this.Sequence = this.Sequence == Int32.MaxValue ? 0 : this.Sequence + 1;

                            _actionHistory.Add(action);
                        }
                    }

                }

            }

            return _clusterClient.BroadcastDirectoryOperation(remoteOperations);

        }

    }

    public class ClusterNodeHubDirectoryHistory
    {

        int _depth;
        int _highWaterMark;

        List<DirectoryAction> _stack;

        object _syncObject = new object();

        public ClusterNodeHubDirectoryHistory(int depth =8096)
        {
            _depth = depth;
            _highWaterMark = (int)(_depth * 1.5);
            _stack = new List<DirectoryAction>(_highWaterMark);
        }

        public void Add(DirectoryAction action)
        {
            lock (_syncObject)
            {
                _stack.Add(action);

                if (_stack.Count> _highWaterMark)
                {
                    _stack = _stack.Skip(_stack.Count - _depth).ToList();
                }
            }
        }

    }



}
