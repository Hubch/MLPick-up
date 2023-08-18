
namespace MLPickup.Modeler.Agents
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using MLPickup.Common.Concurrency;
    using MLPickup.Common.Utilities;

    public interface IAgentHandlerContext : IAttributeMap
    {
        IAgent Agent { get; }

        IByteBufferAllocator Allocator { get; }

        /// <summary>
        /// Returns the <see cref="IEventExecutor"/> which is used to execute an arbitrary task.
        /// </summary>
        IEventExecutor Executor { get; }

        /// <summary>
        /// The unique name of the <see cref="IAgentHandlerContext"/>.
        /// </summary>
        /// <remarks>
        /// The name was used when the <see cref="IAgentHandler"/> was added to the <see cref="IAgentPipeline"/>.
        /// This name can also be used to access the registered <see cref="IAgentHandler"/> from the
        /// <see cref="IAgentPipeline"/>.
        /// </remarks>
        string Name { get; }

        IAgentHandler Handler { get; }

        bool Removed { get; }

        /// <summary>
        /// A <see cref="IAgent"/> was registered to its <see cref="IEventLoop"/>. This will result in having the
        /// <see cref="IAgentHandler.AgentRegistered"/> method called of the next <see cref="IAgentHandler"/>
        /// contained in the <see cref="IAgentPipeline"/> of the <see cref="IAgent"/>.
        /// </summary>
        /// <returns>The current <see cref="IAgentHandlerContext"/>.</returns>
        IAgentHandlerContext FireAgentRegistered();

        /// <summary>
        /// A <see cref="IAgent"/> was unregistered from its <see cref="IEventLoop"/>. This will result in having the
        /// <see cref="IAgentHandler.AgentUnregistered"/> method called of the next <see cref="IAgentHandler"/>
        /// contained in the <see cref="IAgentPipeline"/> of the <see cref="IAgent"/>.
        /// </summary>
        /// <returns>The current <see cref="IAgentHandlerContext"/>.</returns>
        IAgentHandlerContext FireAgentUnregistered();

        IAgentHandlerContext FireAgentActive();

        IAgentHandlerContext FireAgentInactive();

        IAgentHandlerContext FireAgentRead(object message);

        IAgentHandlerContext FireAgentReadComplete();

        IAgentHandlerContext FireAgentWritabilityChanged();

        IAgentHandlerContext FireExceptionCaught(Exception ex);

        IAgentHandlerContext FireUserEventTriggered(object evt);

        IAgentHandlerContext Read();

        Task WriteAsync(object message); // todo: optimize: add flag saying if handler is interested in task, do not produce task if it isn't needed

        IAgentHandlerContext Flush();

        Task WriteAndFlushAsync(object message);

        /// <summary>
        /// Request to bind to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.BindAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> to bind to.</param>
        /// <returns>An await-able task.</returns>
        Task BindAsync(EndPoint localAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.ConnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The <see cref="EndPoint"/> to connect to.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress);

        /// <summary>
        /// Request to connect to the given <see cref="EndPoint"/> while also binding to the localAddress.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.ConnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <param name="remoteAddress">The <see cref="EndPoint"/> to connect to.</param>
        /// <param name="localAddress">The <see cref="EndPoint"/> to bind to.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(EndPoint remoteAddress, EndPoint localAddress);

        /// <summary>
        /// Request to disconnect from the remote peer.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.DisconnectAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DisconnectAsync();

        Task CloseAsync();

        /// <summary>
        /// Request to deregister from the previous assigned <see cref="IEventExecutor"/>.
        /// <para>
        /// This will result in having the <see cref="IAgentHandler.DeregisterAsync"/> method called of the next
        /// <see cref="IAgentHandler"/> contained in the <see cref="IAgentPipeline"/> of the
        /// <see cref="IAgent"/>.
        /// </para>
        /// </summary>
        /// <returns>An await-able task.</returns>
        Task DeregisterAsync();
    }
}
