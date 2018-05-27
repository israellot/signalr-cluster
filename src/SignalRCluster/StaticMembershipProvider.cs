using Microsoft.Extensions.Options;
using SignalRCluster.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster
{
    public class StaticMembershipProvider : IClusterSignalRMembershipProvider
    {
        StaticClusterMembershipOptions _membershipOptions;

        ClusterOptions _clusterOptions;

        Lazy<string> _clusterNodeUniqueId;

        public StaticMembershipProvider(IOptions<ClusterOptions> clusterOptions, IOptions<StaticClusterMembershipOptions> membershipOptions)
            : this(clusterOptions.Value, membershipOptions.Value)
        {


        }

        public StaticMembershipProvider(ClusterOptions clusterOptions, StaticClusterMembershipOptions membershipOptions)
        {
            _membershipOptions = membershipOptions;
            _clusterOptions = clusterOptions;

            // Use the machine name for convenient diagnostics, but add a guid to make it unique.
            // Example: MyServerName_02db60e5fab243b890a847fa5c4dcb29
            _clusterNodeUniqueId = new Lazy<string>(() => $"{Environment.MachineName}_{Guid.NewGuid():N}");

        }

        public string ClusterToken { get { return _clusterOptions.ClusterToken; } }

        public Task Announce(string nodeId, string address)
        {
            return Task.FromResult(0);
        }

        public Task<IList<ClusterMember>> GetClusterMembersAsync()
        {
            //just return all static members as if they just announced themselves
            var members = _membershipOptions.Members.Select(m => new ClusterMember()
            {
                Address = m.Address,
                Name = m.Name,
                LastAnnouncement = DateTimeOffset.UtcNow
            }).ToList();

            return Task.FromResult((IList<ClusterMember>)members);
        }

        public string GetOwnUniqueId()
        {
            return _clusterNodeUniqueId.Value;
        }
    }

    public class StaticClusterMembershipOptions
    {
        public List<StaticClusterMember> Members { get; set; }
    }

    public class StaticClusterMember
    {

        public string Name { get; set; }

        public string Address { get; set; }

        public StaticClusterMember() { }

        public StaticClusterMember(string name, string address)
        {
            this.Name = name;
            this.Address = address;
        }

    }
}
