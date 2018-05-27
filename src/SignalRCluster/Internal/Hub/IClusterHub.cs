using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SignalRCluster.Internal
{
    public interface IClusterHub
    {
        Task RemoteUniqueId(string id);
    }
}
