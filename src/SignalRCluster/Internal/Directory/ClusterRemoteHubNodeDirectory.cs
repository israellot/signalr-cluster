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

    public class ClusterRemoteHubNodeDirectory
    {

        private readonly ConcurrentDictionary<string, string> _connections = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _users = new ConcurrentDictionary<string, ImmutableHashSet<string>>();
        private readonly ConcurrentDictionary<string, ImmutableHashSet<string>> _groups = new ConcurrentDictionary<string, ImmutableHashSet<string>>();
                       

        public ClusterRemoteHubNodeDirectory()
        {
            
        }

        public void PerformAction(RemoteDirectoryOperation operation)
        {

            var action = operation.Action;
            var node = operation.Node;

            if (action.Element == DirectoryElement.Connection)
            {
                if (action.Action == DirectoryElementAction.Add)
                    _connections.AddOrUpdate(action.Item, node, (_, __) => node);
                else
                    _connections.TryRemove(action.Item, out _);

                return;
            }

            if (action.Element == DirectoryElement.User)
            {
                var user = action.Item;

                var hashSet = _users.GetOrAdd(user, ImmutableHashSet<string>.Empty);

                if (action.Action == DirectoryElementAction.Add)
                {
                    _users.AddOrUpdate(
                       user,
                       ImmutableHashSet<string>.Empty.Add(node),
                       (_, set) => set.Add(node)
                       );
                }
                else
                {
                    if (_users.TryGetValue(user, out var set))
                    {

                        var newSet = set.Remove(node);
                        if (newSet == ImmutableHashSet<string>.Empty)
                        {
                            _users.TryRemove(user, out _);
                        }
                        else
                        {
                            _users.TryUpdate(user, set, newSet);
                        }
                    }
                }

                return;
            }

            if (action.Element == DirectoryElement.Group)
            {
                var group = action.Item;

                var hashSet = _groups.GetOrAdd(group, ImmutableHashSet<string>.Empty);

                if (action.Action == DirectoryElementAction.Add)
                {
                    _users.AddOrUpdate(
                       group,
                       ImmutableHashSet<string>.Empty.Add(node),
                       (_, set) => set.Add(node)
                       );
                }
                else
                {
                    if (_users.TryGetValue(group, out var set))
                    {

                        var newSet = set.Remove(node);
                        if (newSet == ImmutableHashSet<string>.Empty)
                        {
                            _users.TryRemove(group, out _);
                        }
                        else
                        {
                            _users.TryUpdate(group, set, newSet);
                        }
                    }
                }

                return;
            }

        }

        public string[] AllNodes()
        {
            return _connections.Values.ToArray();
        }

    }




}
