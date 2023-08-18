using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLModeling.Modeler.Bootstrapping
{
    using System.Net;
    using System.Threading.Tasks;

    public interface INameResolver
    {
        bool IsResolved(EndPoint address);

        Task<EndPoint> ResolveAsync(EndPoint address);
    }
}
