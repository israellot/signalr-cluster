using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public class ClusterPeerNodeClientFactory
    {
        private IClusterSignalRMembershipProvider _membershipProvider;
        private IServiceProvider _serviceProvider;

        public ClusterPeerNodeClientFactory(IServiceProvider serviceProvider,
            IClusterSignalRMembershipProvider membershipProvider)
        {
            _serviceProvider = serviceProvider;
            _membershipProvider = membershipProvider;
        }

        public ClusterPeerNodeClient GetNewClient(string name, string address)
        {
            return new ClusterPeerNodeClient(_membershipProvider, name, address);
        }

    }

    public class ClusterPeerNodeClient : IDisposable
    {
        private bool _isDisposed;

        private IClusterSignalRMembershipProvider _membershipProvider;

        public string Name { get; private set; }

        public string UniqueId { get; set; }

        public string Address { get; private set; }

        public Boolean IsSelf { get; set; }

        public HubConnection Connection { get; private set; }

        public Boolean IsConnected { get; private set; }

        public ClusterPeerNodeClient(
            IClusterSignalRMembershipProvider membershipProvider,
            string name, string address)
        {
            Name = name;
            Address = address;

            _membershipProvider = membershipProvider;

            var ignore = ConnectTask();
        }


        private async Task ConnectTask()
        {
            if (Connection == null && !IsSelf)
            {
                var builder = new HubConnectionBuilder()
                .WithUrl(Address + "/_cluster", HttpTransportType.WebSockets, o => {
                                        
                    o.Headers.Add("X-SignalR-Cluster-Token", _membershipProvider.ClusterToken);
                    o.Headers.Add("X-SignalR-Cluster-Node", Name);
                    o.Headers.Add("X-SignalR-Cluster-Node-Id", _membershipProvider.GetOwnUniqueId());

                })
                .AddMessagePackProtocol();

                Connection = builder.Build();

                Connection.Closed += (ex) => {
                    Connection = null;
                    IsConnected = false;
                    return ConnectTask();
                };

                while (!_isDisposed)
                {
                    try
                    {
                        await Connection.StartAsync();
                        IsConnected = true;

                        
                        var remoteUniqueId = await Connection.InvokeAsync<string>("GetUniqueId");
                        if (remoteUniqueId == _membershipProvider.GetOwnUniqueId())
                        {
                            IsSelf = true;
                            Connection.StopAsync();
                        }
                        else
                        {
                            UniqueId = remoteUniqueId;
                        }

                        break;
                    }
                    catch
                    {
                        //ignore?
                    }
                    await Task.Delay(1000);
                }

            }


        }

       

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
