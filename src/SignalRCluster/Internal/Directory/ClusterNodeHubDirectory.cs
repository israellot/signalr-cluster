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

  
    public class ClusterNodeHubDirectory
    {

        private ImmutableHashSet<string> _connections = ImmutableHashSet<string>.Empty;
        private ImmutableHashSet<string> _users = ImmutableHashSet<string>.Empty;
        private ImmutableHashSet<string> _groups = ImmutableHashSet<string>.Empty;

        

        public ClusterNodeHubDirectory()
        {
            
        }

        public bool PerformOperation(DirectoryAction action)
        {

            ImmutableHashSet<string> hashset;
            switch (action.Element)
            {
                case DirectoryElement.User: hashset = _users; break;
                case DirectoryElement.Connection: hashset = _connections; break;
                case DirectoryElement.Group: hashset = _groups; break;
                default:
                    throw new NotImplementedException(); //TODO proper exception
            }

            ImmutableHashSet<string> newHashSet = hashset;

            if (action.Action == DirectoryElementAction.Add)
                newHashSet = hashset.Add(action.Item);
            else
                newHashSet = hashset.Remove(action.Item);

            if (newHashSet != hashset)
            {
                switch (action.Element)
                {
                    case DirectoryElement.User: Interlocked.Exchange(ref _users, newHashSet); break;
                    case DirectoryElement.Connection: Interlocked.Exchange(ref _connections, newHashSet); break;
                    case DirectoryElement.Group: Interlocked.Exchange(ref _groups, newHashSet); break;
                }

                return true;
            }

            return false;
        }
                
        public bool HasConnection(string connectionId)
        {
           return _connections.Contains(connectionId);
        }

        public bool HasUser(string userId)
        {
            return _users.Contains(userId);
        }

        public bool HasGroup(string group)
        {
            return _groups.Contains(group);
        }

    }


}
