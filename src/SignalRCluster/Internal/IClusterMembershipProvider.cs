using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public interface IClusterSignalRMembershipProvider
    {        
        Task<IList<ClusterMember>> GetClusterMembersAsync();

        string GetOwnUniqueId();

        string ClusterToken { get; }
    }
}

