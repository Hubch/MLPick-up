
namespace MLModeling.Modeler.Bootstrapping
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MLModeling.Common.Internal.Logging;
    using MLModeling.Common.Utilities;
    using MLModeling.Modeler.Agents;

    /// <summary>
    /// A <see cref="Bootstrap"/> sub-class which allows easy bootstrapping of <see cref="IServerAgent"/>.
    /// </summary>
    public class ServerBootstrap : AbstractBootstrap<ServerBootstrap, IServerAgent>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<ServerBootstrap>();

        readonly ConcurrentDictionary<AgentOption, AgentOptionValue> childOptions;
        readonly ConcurrentDictionary<IConstant, AttributeValue> childAttrs;
        volatile IEventLoopGroup childGroup;
        volatile IAgentHandler childHandler;

        public ServerBootstrap()
        {
            this.childOptions = new ConcurrentDictionary<AgentOption, AgentOptionValue>();
            this.childAttrs = new ConcurrentDictionary<IConstant, AttributeValue>();
        }

        ServerBootstrap(ServerBootstrap bootstrap)
            : base(bootstrap)
        {
            this.childGroup = bootstrap.childGroup;
            this.childHandler = bootstrap.childHandler;
            this.childOptions = new ConcurrentDictionary<AgentOption, AgentOptionValue>(bootstrap.childOptions);
            this.childAttrs = new ConcurrentDictionary<IConstant, AttributeValue>(bootstrap.childAttrs);
        }

        /// <summary>
        /// Specifies the <see cref="IEventLoopGroup"/> which is used for the parent (acceptor) and the child (client).
        /// </summary>
        public override ServerBootstrap Group(IEventLoopGroup group) => this.Group(group, group);

        /// <summary>
        /// Sets the <see cref="IEventLoopGroup"/> for the parent (acceptor) and the child (client). These
        /// <see cref="IEventLoopGroup"/>'s are used to handle all the events and IO for <see cref="IServerAgent"/>
        /// and <see cref="IAgent"/>'s.
        /// </summary>
        public ServerBootstrap Group(IEventLoopGroup parentGroup, IEventLoopGroup childGroup)
        {
            Contract.Requires(childGroup != null);

            base.Group(parentGroup);
            if (this.childGroup != null)
            {
                throw new InvalidOperationException("childGroup set already");
            }
            this.childGroup = childGroup;
            return this;
        }

        /// <summary>
        /// Allows specification of a <see cref="AgentOption"/> which is used for the <see cref="IAgent"/>
        /// instances once they get created (after the acceptor accepted the <see cref="IAgent"/>). Use a
        /// value of <c>null</c> to remove a previously set <see cref="AgentOption"/>.
        /// </summary>
        public ServerBootstrap ChildOption<T>(AgentOption<T> childOption, T value)
        {
            Contract.Requires(childOption != null);

            if (value == null)
            {
                AgentOptionValue removed;
                this.childOptions.TryRemove(childOption, out removed);
            }
            else
            {
                this.childOptions[childOption] = new AgentOptionValue<T>(childOption, value);
            }
            return this;
        }

        /// <summary>
        /// Sets the specific <see cref="AttributeKey{T}"/> with the given value on every child <see cref="IAgent"/>.
        /// If the value is <c>null</c>, the <see cref="AttributeKey{T}"/> is removed.
        /// </summary>
        public ServerBootstrap ChildAttribute<T>(AttributeKey<T> childKey, T value)
            where T : class
        {
            Contract.Requires(childKey != null);

            if (value == null)
            {
                AttributeValue removed;
                this.childAttrs.TryRemove(childKey, out removed);
            }
            else
            {
                this.childAttrs[childKey] = new AttributeValue<T>(childKey, value);
            }
            return this;
        }

        /// <summary>
        /// Sets the <see cref="IAgentHandler"/> which is used to serve the request for the <see cref="IAgent"/>'s.
        /// </summary>
        public ServerBootstrap ChildHandler(IAgentHandler childHandler)
        {
            Contract.Requires(childHandler != null);

            this.childHandler = childHandler;
            return this;
        }

        /// <summary>
        /// Returns the configured <see cref="IEventLoopGroup"/> which will be used for the child Agents or <c>null</c>
        /// if none is configured yet.
        /// </summary>
        public IEventLoopGroup ChildGroup() => this.childGroup;

        protected override void Init(IAgent Agent)
        {
            SetAgentOptions(Agent, this.Options, Logger);

            foreach (AttributeValue e in this.Attributes)
            {
                e.Set(Agent);
            }

            IAgentPipeline p = Agent.Pipeline;
            IAgentHandler AgentHandler = this.Handler();
            if (AgentHandler != null)
            {
                p.AddLast((string)null, AgentHandler);
            }

            IEventLoopGroup currentChildGroup = this.childGroup;
            IAgentHandler currentChildHandler = this.childHandler;
            AgentOptionValue[] currentChildOptions = this.childOptions.Values.ToArray();
            AttributeValue[] currentChildAttrs = this.childAttrs.Values.ToArray();

            p.AddLast(new ActionAgentInitializer<IAgent>(ch =>
            {
                ch.Pipeline.AddLast(new ServerBootstrapAcceptor(currentChildGroup, currentChildHandler,
                    currentChildOptions, currentChildAttrs));
            }));
        }

        public override ServerBootstrap Validate()
        {
            base.Validate();
            if (this.childHandler == null)
            {
                throw new InvalidOperationException("childHandler not set");
            }
            if (this.childGroup == null)
            {
                Logger.Warn("childGroup is not set. Using parentGroup instead.");
                this.childGroup = this.Group();
            }
            return this;
        }

        class ServerBootstrapAcceptor : AgentHandlerAdapter
        {
            readonly IEventLoopGroup childGroup;
            readonly IAgentHandler childHandler;
            readonly AgentOptionValue[] childOptions;
            readonly AttributeValue[] childAttrs;

            public ServerBootstrapAcceptor(
                IEventLoopGroup childGroup, IAgentHandler childHandler,
                AgentOptionValue[] childOptions, AttributeValue[] childAttrs)
            {
                this.childGroup = childGroup;
                this.childHandler = childHandler;
                this.childOptions = childOptions;
                this.childAttrs = childAttrs;
            }

            public override void AgentRead(IAgentHandlerContext ctx, object msg)
            {
                var child = (IAgent)msg;

                child.Pipeline.AddLast((string)null, this.childHandler);

                SetAgentOptions(child, this.childOptions, Logger);

                foreach (AttributeValue attr in this.childAttrs)
                {
                    attr.Set(child);
                }

                // todo: async/await instead?
                try
                {
                    this.childGroup.RegisterAsync(child).ContinueWith(
                        (future, state) => ForceClose((IAgent)state, future.Exception),
                        child,
                        TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously);
                }
                catch (Exception ex)
                {
                    ForceClose(child, ex);
                }
            }

            static void ForceClose(IAgent child, Exception ex)
            {
                child.Unsafe.CloseForcibly();
                Logger.Warn("Failed to register an accepted Agent: " + child, ex);
            }

            public override void ExceptionCaught(IAgentHandlerContext ctx, Exception cause)
            {
                IAgentConfiguration config = ctx.Agent.Configuration;
                if (config.AutoRead)
                {
                    // stop accept new connections for 1 second to allow the Agent to recover
                    // See https://github.com/netty/netty/issues/1328
                    config.AutoRead = false;
                    ctx.Agent.EventLoop.ScheduleAsync(c => { ((IAgentConfiguration)c).AutoRead = true; }, config, TimeSpan.FromSeconds(1));
                }
                // still let the ExceptionCaught event flow through the pipeline to give the user
                // a chance to do something with it
                ctx.FireExceptionCaught(cause);
            }
        }

        public override ServerBootstrap Clone() => new ServerBootstrap(this);

        public override string ToString()
        {
            var buf = new StringBuilder(base.ToString());
            buf.Length = buf.Length - 1;
            buf.Append(", ");
            if (this.childGroup != null)
            {
                buf.Append("childGroup: ")
                    .Append(this.childGroup.GetType().Name)
                    .Append(", ");
            }
            buf.Append("childOptions: ")
                .Append(this.childOptions.ToDebugString())
                .Append(", ");
            // todo: attrs
            //lock (childAttrs)
            //{
            //    if (!childAttrs.isEmpty())
            //    {
            //        buf.Append("childAttrs: ");
            //        buf.Append(childAttrs);
            //        buf.Append(", ");
            //    }
            //}
            if (this.childHandler != null)
            {
                buf.Append("childHandler: ");
                buf.Append(this.childHandler);
                buf.Append(", ");
            }
            if (buf[buf.Length - 1] == '(')
            {
                buf.Append(')');
            }
            else
            {
                buf[buf.Length - 2] = ')';
                buf.Length = buf.Length - 1;
            }

            return buf.ToString();
        }
    }
}
