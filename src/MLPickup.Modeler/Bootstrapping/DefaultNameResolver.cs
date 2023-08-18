using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLPickup.Modeler.Bootstrapping
{
    using System.Net;
    using System.Threading.Tasks;

    public class DefaultNameResolver : INameResolver
    {
        public bool IsResolved(EndPoint address) => !(address is DnsEndPoint);

        public async Task<EndPoint> ResolveAsync(EndPoint address)
        {
            var asDns = address as DnsEndPoint;
            if (asDns != null)
            {
                IPHostEntry resolved = await Dns.GetHostEntryAsync(asDns.Host);
                return new IPEndPoint(resolved.AddressList[0], asDns.Port);
            }
            else
            {
                return address;
            }
        }
    }
}
