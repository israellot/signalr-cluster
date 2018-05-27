using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using SignalRCluster.Internal;

namespace SignalRCluster
{
    /// <summary>
    /// Extension methods for configuring Redis-based scale-out for a SignalR Server in an <see cref="ISignalRServerBuilder" />.
    /// </summary>
    public static class ClusterDependencyInjectionExtensions
    {

        public interface IClusterSignalRBuilder : ISignalRServerBuilder { }

        public class ClusterSignalRBuilder:IClusterSignalRBuilder
        {
            ISignalRServerBuilder _builder;

            public ClusterSignalRBuilder(ISignalRServerBuilder builder)
            {
                _builder = builder;

                
            }

            public IServiceCollection Services => _builder.Services;

            
        }

        public static IClusterSignalRBuilder AddCluster(this ISignalRServerBuilder signalrBuilder)
        {
            return AddCluster(signalrBuilder, c => { });
        }


        public static IClusterSignalRBuilder AddCluster(this ISignalRServerBuilder signalrBuilder, Action<ClusterOptions> configure)
        {

            signalrBuilder.Services.Configure(configure);
            signalrBuilder.Services.AddSingleton<ClusterSignalRMarkerService>();
            signalrBuilder.Services.AddSingleton<ClusterManager>();
            signalrBuilder.Services.AddSingleton<ClusterClient>();
            signalrBuilder.Services.AddSingleton<ClusterHub>();
            signalrBuilder.Services.AddSingleton<ClusterRemoteDirectory>();
            signalrBuilder.Services.AddSingleton<ClusterLocalNodeDirectory>();
            signalrBuilder.Services.AddSingleton<ClusterPeerNodeClientFactory>();
            signalrBuilder.Services.AddSingleton<ClusterHubLifetimeManagerCache>();
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<ClusterHub>), typeof(DefaultHubLifetimeManager<ClusterHub>));
            signalrBuilder.Services.AddSingleton(typeof(HubLifetimeManager<>), typeof(ClusterHubLifetimeManager<>));

            return new ClusterSignalRBuilder(signalrBuilder);
        }

        public static IClusterSignalRBuilder AddClusterStaticMembership(this IClusterSignalRBuilder signalrBuilder, StaticClusterMember[] clusterMembers)
        {
            signalrBuilder.Services
                .Configure<StaticClusterMembershipOptions>(s =>
                {
                    s.Members = clusterMembers.ToList();
                })
                .AddSingleton<IClusterSignalRMembershipProvider, StaticMembershipProvider>();

            return signalrBuilder;
        }

    }
}
