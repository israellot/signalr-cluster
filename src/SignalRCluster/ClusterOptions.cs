using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;


namespace SignalRCluster
{
    /// <summary>
    /// Options used to configure <see cref="ClusterHubLifetimeManager{THub}"/>.
    /// </summary>
    public class ClusterOptions
    {
        public String ClusterToken { get; set; } = "secrettoken";

    }
}
