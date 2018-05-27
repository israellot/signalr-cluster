// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR;
using SignalRCluster.Internal;

namespace SignalRCluster
{


    /// <summary>
    /// A default in-memory lifetime manager abstraction for <see cref="Hub"/> instances.
    /// </summary>
    public class ClusterHubLifetimeManager<THub> : HubLifetimeManager<THub>, IClusterHubLifetimeManager where THub : Hub
    {


        private readonly HubConnectionStore _connections = new HubConnectionStore();
        private readonly ClusterHubGroupList _groups = new ClusterHubGroupList();
        private readonly ILogger _logger;
        private readonly IClusterSignalRMembershipProvider _membershipProvider;
        private readonly ClusterLocalNodeDirectory _localDirectory;
        private readonly ClusterRemoteDirectory _remoteDirectory;
        private readonly ClusterClient _clusterClient;
        private readonly string _hubName;

        private string _nodeUniqueId => _membershipProvider.GetOwnUniqueId();


        public ClusterHubLifetimeManager(
            ILogger<DefaultHubLifetimeManager<THub>> logger,
            IClusterSignalRMembershipProvider membershipProvider,
            ClusterLocalNodeDirectory localDirectory,
            ClusterRemoteDirectory remoteDirectory,
            ClusterClient clusterClient,
            ClusterHubLifetimeManagerCache lifetimeManagerCache
            )
        {
            _logger = logger;
            _membershipProvider = membershipProvider;

            _localDirectory = localDirectory;
            _remoteDirectory = remoteDirectory;

            _clusterClient = clusterClient;

            _hubName = typeof(THub).FullName;

            lifetimeManagerCache.Add(_hubName, this);
        }

        /// <inheritdoc />
        public override Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            if (_groups.Add(connection, groupName))
            {
                //new group                
                return _localDirectory.PerformAction(DirectoryAction.AddGroup(_hubName, groupName));
            }


            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var connection = _connections[connectionId];
            if (connection == null)
            {
                return Task.CompletedTask;
            }

            if (_groups.Remove(connectionId, groupName))
            {
                //empty group
                return _localDirectory.PerformAction(DirectoryAction.RemoveGroup(_hubName, groupName));
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendAllAsync(string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args);
        }

        public Task SendToAllLocalConnection(string methodName, object[] args, string[] excludeConnectionIds = null)
        {
            List<Task> tasks = null;
            SerializedHubMessage message = null;

            // foreach over HubConnectionStore avoids allocating an enumerator
            foreach (var connection in _connections)
            {
                if (excludeConnectionIds != null && excludeConnectionIds.Contains(connection.ConnectionId))
                    continue;

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.WriteAsync(message);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }

            if (tasks == null)
            {
                return Task.CompletedTask;
            }



            // Some connections are slow
            return Task.WhenAll(tasks);
        }

        private Task SendToAllConnections(string methodName, object[] args, Func<HubConnectionContext, bool> include)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(SendToAllRemoteConnections(methodName, args));
            tasks.Add(SendToAllLocalConnection(methodName, args));


            // Some connections are slow
            return Task.WhenAll(tasks);
        }

        private Task SendToAllConnections(string methodName, object[] args, string[] excludeConnectionIds = null)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(SendToAllRemoteConnections(methodName, args, excludeConnectionIds));
            tasks.Add(SendToAllLocalConnection(methodName, args, excludeConnectionIds));


            // Some connections are slow
            return Task.WhenAll(tasks);
        }

        private Task SendToAllRemoteConnections(string methodName, object[] args, string[] excludeConnectionIds = null)
        {
            var nodes = _remoteDirectory.AllNodes(_hubName);

            return _clusterClient.SendToAll(nodes, _hubName, methodName, args, excludeConnectionIds);
        }

        // Tasks and message are passed by ref so they can be lazily created inside the method post-filtering,
        // while still being re-usable when sending to multiple groups
        private void SendToGroupConnections(string methodName, object[] args, ConcurrentDictionary<string, HubConnectionContext> connections, Func<HubConnectionContext, bool> include, ref List<Task> tasks, ref SerializedHubMessage message)
        {
            // foreach over ConcurrentDictionary avoids allocating an enumerator
            foreach (var connection in connections)
            {
                if (include != null && !include(connection.Value))
                {
                    continue;
                }

                if (message == null)
                {
                    message = CreateSerializedInvocationMessage(methodName, args);
                }

                var task = connection.Value.WriteAsync(message);

                if (!task.IsCompletedSuccessfully)
                {
                    if (tasks == null)
                    {
                        tasks = new List<Task>();
                    }

                    tasks.Add(task.AsTask());
                }
            }
        }

        /// <inheritdoc />
        public override Task SendConnectionAsync(string connectionId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (connectionId == null)
            {
                throw new ArgumentNullException(nameof(connectionId));
            }

            var connection = _connections[connectionId];

            if (connection == null)
            {
                return Task.CompletedTask;
            }

            // We're sending to a single connection
            // Write message directly to connection without caching it in memory
            var message = CreateInvocationMessage(methodName, args);

            return connection.WriteAsync(message).AsTask();
        }

        /// <inheritdoc />
        public override Task SendGroupAsync(string groupName, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                // Can't optimize for sending to a single connection in a group because
                // group might be modified inbetween checking and sending
                List<Task> tasks = null;
                SerializedHubMessage message = null;
                SendToGroupConnections(methodName, args, group, null, ref tasks, ref message);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendGroupsAsync(IReadOnlyList<string> groupNames, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            // Each task represents the list of tasks for each of the writes within a group
            List<Task> tasks = null;
            SerializedHubMessage message = null;

            foreach (var groupName in groupNames)
            {
                if (string.IsNullOrEmpty(groupName))
                {
                    throw new InvalidOperationException("Cannot send to an empty group name.");
                }

                var group = _groups[groupName];
                if (group != null)
                {
                    SendToGroupConnections(methodName, args, group, null, ref tasks, ref message);
                }
            }

            if (tasks != null)
            {
                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task SendGroupExceptAsync(string groupName, string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            if (groupName == null)
            {
                throw new ArgumentNullException(nameof(groupName));
            }

            var group = _groups[groupName];
            if (group != null)
            {
                List<Task> tasks = null;
                SerializedHubMessage message = null;

                SendToGroupConnections(methodName, args, group, connection => !excludedConnectionIds.Contains(connection.ConnectionId), ref tasks, ref message);

                if (tasks != null)
                {
                    return Task.WhenAll(tasks);
                }
            }

            return Task.CompletedTask;
        }

        private SerializedHubMessage CreateSerializedInvocationMessage(string methodName, object[] args)
        {
            return new SerializedHubMessage(CreateInvocationMessage(methodName, args));
        }

        private HubMessage CreateInvocationMessage(string methodName, object[] args)
        {
            return new InvocationMessage(methodName, args);
        }

        /// <inheritdoc />
        public override Task SendUserAsync(string userId, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, connection => string.Equals(connection.UserIdentifier, userId, StringComparison.Ordinal));
        }

        /// <inheritdoc />
        public override Task OnConnectedAsync(HubConnectionContext connection)
        {
            _connections.Add(connection);

            var actions = new List<DirectoryAction>();
            actions.Add(DirectoryAction.AddConnection(_hubName, connection.ConnectionId));

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
                actions.Add(DirectoryAction.AddUser(_hubName, connection.UserIdentifier));

            return _localDirectory.PerformActions(actions);

        }

        /// <inheritdoc />
        public override Task OnDisconnectedAsync(HubConnectionContext connection)
        {
            _connections.Remove(connection);
            var emptyGroups = _groups.RemoveDisconnectedConnection(connection.ConnectionId);

            var actions = new List<DirectoryAction>();
            actions.Add(DirectoryAction.RemoveConnection(_hubName, connection.ConnectionId));

            if (!string.IsNullOrEmpty(connection.UserIdentifier))
                actions.Add(DirectoryAction.RemoveUser(_hubName, connection.UserIdentifier));

            actions.AddRange(emptyGroups.Select(g => DirectoryAction.RemoveGroup(_hubName, g)));

            return _localDirectory.PerformActions(actions);

        }

        /// <inheritdoc />
        public override Task SendAllExceptAsync(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, excludedConnectionIds.ToArray());
        }

        /// <inheritdoc />
        public override Task SendConnectionsAsync(IReadOnlyList<string> connectionIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, connection => connectionIds.Contains(connection.ConnectionId));
        }

        /// <inheritdoc />
        public override Task SendUsersAsync(IReadOnlyList<string> userIds, string methodName, object[] args, CancellationToken cancellationToken = default)
        {
            return SendToAllConnections(methodName, args, connection => userIds.Contains(connection.UserIdentifier));
        }
    }


    public interface IClusterHubLifetimeManager
    {
        Task SendToAllLocalConnection(string methodName, object[] args, string[] excludeConnectionIds = null);
    }
}
