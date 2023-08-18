
namespace MLPickup.Modeler.Agents
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using MLPickup.Common.Utilities;

    public class AgentHandlerAdapter : IAgentHandler
    {
        internal bool Added;

        [Skip]
        public virtual void AgentRegistered(IAgentHandlerContext context) => context.FireAgentRegistered();

        [Skip]
        public virtual void AgentUnregistered(IAgentHandlerContext context) => context.FireAgentUnregistered();

        [Skip]
        public virtual void AgentActive(IAgentHandlerContext context) => context.FireAgentActive();

        [Skip]
        public virtual void AgentInactive(IAgentHandlerContext context) => context.FireAgentInactive();

        [Skip]
        public virtual void AgentRead(IAgentHandlerContext context, object message) => context.FireAgentRead(message);

        [Skip]
        public virtual void AgentReadComplete(IAgentHandlerContext context) => context.FireAgentReadComplete();

        [Skip]
        public virtual void AgentWritabilityChanged(IAgentHandlerContext context) => context.FireAgentWritabilityChanged();

        [Skip]
        public virtual void HandlerAdded(IAgentHandlerContext context)
        {
        }

        [Skip]
        public virtual void HandlerRemoved(IAgentHandlerContext context)
        {
        }

        [Skip]
        public virtual void UserEventTriggered(IAgentHandlerContext context, object evt) => context.FireUserEventTriggered(evt);

        [Skip]
        public virtual Task WriteAsync(IAgentHandlerContext context, object message) => context.WriteAsync(message);

        [Skip]
        public virtual void Flush(IAgentHandlerContext context) => context.Flush();

        [Skip]
        public virtual Task BindAsync(IAgentHandlerContext context, EndPoint localAddress) => context.BindAsync(localAddress);

        [Skip]
        public virtual Task ConnectAsync(IAgentHandlerContext context, EndPoint remoteAddress, EndPoint localAddress) => context.ConnectAsync(remoteAddress, localAddress);

        [Skip]
        public virtual Task DisconnectAsync(IAgentHandlerContext context) => context.DisconnectAsync();

        [Skip]
        public virtual Task CloseAsync(IAgentHandlerContext context) => context.CloseAsync();

        [Skip]
        public virtual void ExceptionCaught(IAgentHandlerContext context, Exception exception) => context.FireExceptionCaught(exception);

        [Skip]
        public virtual Task DeregisterAsync(IAgentHandlerContext context) => context.DeregisterAsync();

        [Skip]
        public virtual void Read(IAgentHandlerContext context) => context.Read();

        public virtual bool IsSharable => false;

        protected void EnsureNotSharable()
        {
            if (this.IsSharable)
            {
                throw new InvalidOperationException($"AgentHandler {StringUtil.SimpleClassName(this)} is not allowed to be shared");
            }
        }
    }
}
