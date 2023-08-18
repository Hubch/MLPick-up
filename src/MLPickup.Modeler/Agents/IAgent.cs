
namespace MLPickup.Modeler.Agents
{
    using MLPickup.Common.Utilities;
    using System;
    using System.Net;
    using System.Threading.Tasks;

    public interface IAgent : IAttributeMap, IComparable<IAgent>
    {
        IAgentId Id { get; }

        IByteBufferAllocator Allocator { get; }

        IEventLoop EventLoop { get; }

        IAgent Parent { get; }

        bool Open { get; }

        bool Active { get; }

        bool Registered { get; }

        /// <summary>
        ///     Return the <see cref="ChannelMetadata" /> of the <see cref="IChannel" /> which describe the nature of the
        ///     <see cref="IChannel" />.
        /// </summary>
        AgentMetadata Metadata { get; }

        EndPoint LocalAddress { get; }

        EndPoint RemoteAddress { get; }

        bool IsWritable { get; }

        IAgentUnsafe Unsafe { get; }

        IAgentPipeline Pipeline { get; }

        IAgentConfiguration Configuration { get; }

        Task CloseCompletion { get; }

        Task DeregisterAsync();

        Task BindAsync(EndPoint localAddress);

        Task ConnectAsync(EndPoint remoteAddress);

        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        Task DisconnectAsync();

        Task CloseAsync();

        // todo: make these available through separate interface to hide them from public API on channel

        IAgent Read();

        Task WriteAsync(object message);

        IAgent Flush();

        Task WriteAndFlushAsync(object message);
    }
}
