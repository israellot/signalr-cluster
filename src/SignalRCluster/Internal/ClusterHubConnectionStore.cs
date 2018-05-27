using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SignalRCluster.Internal
{
    public class ClusterHubConnectionStore
    {
        private readonly ConcurrentDictionary<string, HubConnectionContext> _connections =
            new ConcurrentDictionary<string, HubConnectionContext>(StringComparer.Ordinal);

        public HubConnectionContext this[string connectionId]
        {
            get
            {
                _connections.TryGetValue(connectionId, out var connection);
                return connection;
            }
        }

        public int Count => _connections.Count;

        public void Add(HubConnectionContext connection)
        {
            _connections.TryAdd(connection.ConnectionId, connection);
        }

        public void Remove(HubConnectionContext connection)
        {
            _connections.TryRemove(connection.ConnectionId, out _);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public readonly struct Enumerator : IEnumerator<HubConnectionContext>
        {
            private readonly IEnumerator<KeyValuePair<string, HubConnectionContext>> _enumerator;

            public Enumerator(ClusterHubConnectionStore hubConnectionList)
            {
                _enumerator = hubConnectionList._connections.GetEnumerator();
            }

            public HubConnectionContext Current => _enumerator.Current.Value;

            object IEnumerator.Current => Current;

            public void Dispose() => _enumerator.Dispose();

            public bool MoveNext() => _enumerator.MoveNext();

            public void Reset() => _enumerator.Reset();
        }
    }
}
