using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignalRCluster.Internal
{
    public class ClusterRemoteDirectory
    {

        private readonly ConcurrentDictionary<string, ClusterRemoteHubNodeDirectory> _hubs = new ConcurrentDictionary<string, ClusterRemoteHubNodeDirectory>();

        private readonly ConcurrentDictionary<string, int> _sequences = new ConcurrentDictionary<string, int>();

        private readonly ConcurrentDictionary<string, object> _syncObjects = new ConcurrentDictionary<string, object>();

        public ClusterRemoteDirectory()
        {


        }

        public void PerformActions(IEnumerable<RemoteDirectoryOperation> operations)
        {
            foreach(var groupByNode in operations.GroupBy(a=>a.Node))
            {
                var node = groupByNode.Key;

                var syncLock = _syncObjects.GetOrAdd(node, new object());

                lock (syncLock)
                {                   

                    foreach(var remoteAction in groupByNode)
                    {
                        var currentSequence = _sequences.GetOrAdd(node, remoteAction.Sequence);

                        if (currentSequence == remoteAction.Sequence)
                        {
                            var hubDirectory = _hubs.GetOrAdd(remoteAction.Action.Hub, _ => new ClusterRemoteHubNodeDirectory());

                            hubDirectory.PerformAction(remoteAction);

                            _sequences[node] = currentSequence + 1;

                        }
                        else if(remoteAction.Sequence > currentSequence)
                        {
                            //need re-sync


                        }
                        else
                        {
                            //duplicate, discard
                        }

                    }

                }

            }
        }


        public string[] AllNodes(string hubName)
        {
            if (_hubs.TryGetValue(hubName, out var hubDirectory))
            {
                return hubDirectory.AllNodes();
            }
            else
                return new string[] { };
        }
    }
}
