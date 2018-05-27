using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace SignalRCluster
{
    public class ClusterHubLifetimeManagerCache
    {
        private ConcurrentDictionary<string, WeakReference<IClusterHubLifetimeManager>> _lifetimeManagers = new ConcurrentDictionary<string, WeakReference<IClusterHubLifetimeManager>>();
      
        public void Add(string hubName, IClusterHubLifetimeManager manager)
        {
            _lifetimeManagers[hubName] = new WeakReference<IClusterHubLifetimeManager>(manager);
        }

        public IClusterHubLifetimeManager Get(string hubName)
        {
            if(_lifetimeManagers.TryGetValue(hubName,out var reference))
            {
                if (reference.TryGetTarget(out var manager))
                    return manager;
                else
                    _lifetimeManagers.TryRemove(hubName, out _);
            }

            return null;
        }
    }
}
