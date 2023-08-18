
namespace MLPickup.Modeler.Agents
{

    using System;
    using System.Net;
    using System.Threading.Tasks;

    public interface IAgentHandler
    {
        /// <summary>
        /// The <see cref="IAgent"/> of the <see cref="IAgentHandlerContext"/> was registered with its
        /// <see cref="IEventLoop"/>.
        /// </summary>
        void AgentRegistered(IAgentHandlerContext context);

        /// <summary>
        /// The <see cref="IAgent"/> of the <see cref="IAgentHandlerContext"/> was unregistered from its
        /// <see cref="IEventLoop"/>.
        /// </summary>
        void AgentUnregistered(IAgentHandlerContext context);

        void AgentActive(IAgentHandlerContext context);

        void AgentInactive(IAgentHandlerContext context);

        void AgentRead(IAgentHandlerContext context, object message);

        void AgentReadComplete(IAgentHandlerContext context);

        /// <summary>
        /// Gets called once the writable state of a <see cref="IAgent"/> changed. You can check the state with
        /// <see cref="IAgent.IsWritable"/>.
        /// </summary>
        void AgentWritabilityChanged(IAgentHandlerContext context);

        void HandlerAdded(IAgentHandlerContext context);

        void HandlerRemoved(IAgentHandlerContext context);

        Task WriteAsync(IAgentHandlerContext context, object message);

        void Flush(IAgentHandlerContext context);

        /// <summary>
        /// Called once a bind operation is made.
        /// </summary>
        /// <param name="context">
        /// The <see cref="IAgentHandlerContext"/> for which the bind operation is made.
        /// </param>
        /// <param name="localAddress">The <see cref="EndPoint"/> to which it should bind.</param>
        /// <returns>An await-able task.</returns>
        Task BindAsync(IAgentHandlerContext context, EndPoint localAddress);

        /// <summary>
        /// Called once a connect operation is made.
        /// </summary>
        /// <param name="context">
        /// The <see cref="IAgentHandlerContext"/> for which the connect operation is made.
        /// </param>
        /// <param name="remoteAddress">The <see cref="EndPoint"/> to which it should connect.</param>
        /// <param name="localAddress">The <see cref="EndPoint"/> which is used as source on connect.</param>
        /// <returns>An await-able task.</returns>
        Task ConnectAsync(IAgentHandlerContext context, EndPoint remoteAddress, EndPoint localAddress);

        /// <summary>
        /// Called once a disconnect operation is made.
        /// </summary>
        /// <param name="context">
        /// The <see cref="IAgentHandlerContext"/> for which the disconnect operation is made.
        /// </param>
        /// <returns>An await-able task.</returns>
        Task DisconnectAsync(IAgentHandlerContext context);

        Task CloseAsync(IAgentHandlerContext context);

        void ExceptionCaught(IAgentHandlerContext context, Exception exception);

        Task DeregisterAsync(IAgentHandlerContext context);

        void Read(IAgentHandlerContext context);

        void UserEventTriggered(IAgentHandlerContext context, object evt);
    }
}
