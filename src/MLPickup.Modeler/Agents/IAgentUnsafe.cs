using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MLModeling.Modeler.Agents
{
    using System.Net;
    using System.Threading.Tasks;

    public interface IAgentUnsafe
    {
        IRecvByteBufAllocatorHandle RecvBufAllocHandle { get; }

        Task RegisterAsync(IEventLoop eventLoop);

        Task DeregisterAsync();

        Task BindAsync(EndPoint localAddress);

        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        Task DisconnectAsync();

        Task CloseAsync();

        void CloseForcibly();

        void BeginRead();

        Task WriteAsync(object message);

        void Flush();

        AgentOutboundBuffer OutboundBuffer { get; }
    }
}
