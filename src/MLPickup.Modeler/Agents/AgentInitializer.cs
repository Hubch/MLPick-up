
namespace MLPickup.Modeler.Agents
{
    using System;
    using System.Collections.Concurrent;
    using MLPickup.Common.Internal.Logging;

    /// <summary>
    /// A special <see cref="IAgentHandler"/> which offers an easy way to initialize a <see cref="IAgent"/> once it was
    /// registered to its <see cref="IEventLoop"/>.
    /// <para>
    /// Implementations are most often used in the context of <see cref="AbstractBootstrap{TBootstrap,TAgent}.Handler(IAgentHandler)"/>
    /// and <see cref="ServerBootstrap.ChildHandler"/> to setup the <see cref="IAgentPipeline"/> of a <see cref="IAgent"/>.
    /// </para>
    /// Be aware that this class is marked as Sharable (via <see cref="IsSharable"/>) and so the implementation must be safe to be re-used.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyAgentInitializer extends <see cref="AgentInitializer{T}"/> {
    ///     public void InitAgent(<see cref="IAgent"/> channel) {
    ///         channel.Pipeline().AddLast("myHandler", new MyHandler());
    ///     }
    /// }
    /// <see cref="ServerBootstrap"/> bootstrap = ...;
    /// ...
    /// bootstrap.childHandler(new MyAgentInitializer());
    /// ...
    /// </code>
    /// </example>
    /// <typeparam name="T">A sub-type of <see cref="IAgent"/>.</typeparam>
    public abstract class AgentInitializer<T> : AgentHandlerAdapter
        where T : IAgent
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AgentInitializer<T>>();

        readonly ConcurrentDictionary<IAgentHandlerContext, bool> initMap = new ConcurrentDictionary<IAgentHandlerContext, bool>();

        /// <summary>
        /// This method will be called once the <see cref="IAgent"/> was registered. After the method returns this instance
        /// will be removed from the <see cref="IAgentPipeline"/> of the <see cref="IAgent"/>.
        /// </summary>
        /// <param name="channel">The <see cref="IAgent"/> which was registered.</param>
        protected abstract void InitAgent(T channel);

        public override bool IsSharable => true;

        public sealed override void AgentRegistered(IAgentHandlerContext ctx)
        {
            // Normally this method will never be called as handlerAdded(...) should call initAgent(...) and remove
            // the handler.
            if (this.InitAgent(ctx))
            {
                // we called InitAgent(...) so we need to call now pipeline.fireAgentRegistered() to ensure we not
                // miss an event.
                ctx.Agent.Pipeline.FireAgentRegistered();
            }
            else
            {
                // Called InitAgent(...) before which is the expected behavior, so just forward the event.
                ctx.FireAgentRegistered();
            }
        }

        public override void ExceptionCaught(IAgentHandlerContext ctx, Exception cause)
        {
            Logger.Warn("Failed to initialize a channel. Closing: " + ctx.Agent, cause);
            ctx.CloseAsync();
        }

        public override void HandlerAdded(IAgentHandlerContext ctx)
        {
            if (ctx.Agent.Registered)
            {
                // This should always be true with our current DefaultAgentPipeline implementation.
                // The good thing about calling InitAgent(...) in HandlerAdded(...) is that there will be no ordering
                // surprises if a AgentInitializer will add another AgentInitializer. This is as all handlers
                // will be added in the expected order.
                this.InitAgent(ctx);
            }
        }

        bool InitAgent(IAgentHandlerContext ctx)
        {
            if (initMap.TryAdd(ctx, true)) // Guard against re-entrance.
            {
                try
                {
                    this.InitAgent((T)ctx.Agent);
                }
                catch (Exception cause)
                {
                    // Explicitly call exceptionCaught(...) as we removed the handler before calling initAgent(...).
                    // We do so to prevent multiple calls to initAgent(...).
                    this.ExceptionCaught(ctx, cause);
                }
                finally
                {
                    this.Remove(ctx);
                }
                return true;
            }
            return false;
        }

        void Remove(IAgentHandlerContext ctx)
        {
            try
            {
                IAgentPipeline pipeline = ctx.Agent.Pipeline;
                if (pipeline.Context(this) != null)
                {
                    pipeline.Remove(this);
                }
            }
            finally
            {
                initMap.TryRemove(ctx, out bool removed);
            }
        }
    }
}
