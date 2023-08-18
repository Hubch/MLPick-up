
namespace MLModeling.Modeler.Bootstrapping
{
    using MLModeling.Modeler.Agents;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using TaskCompletionSource = MLModeling.Common.Concurrency.TaskCompletionSource;
    using MLModeling.Common.Internal.Logging;
    using MLModeling.Common.Utilities;

    public abstract class AbstractBootstrap<TBootstrap, TAgent>
        where TBootstrap : AbstractBootstrap<TBootstrap, TAgent>
        where TAgent : IAgent
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<AbstractBootstrap<TBootstrap, TAgent>>();

        volatile IEventLoopGroup group;
        volatile Func<TAgent> agentFactory;
        volatile EndPoint localAddress;
        readonly ConcurrentDictionary<AgentOption, AgentOptionValue> options;
        readonly ConcurrentDictionary<IConstant, AttributeValue> attrs;
        volatile IAgentHandler handler;

        protected internal AbstractBootstrap()
        {
            this.options = new ConcurrentDictionary<AgentOption, AgentOptionValue>();
            this.attrs = new ConcurrentDictionary<IConstant, AttributeValue>();
            // Disallow extending from a different package.
        }

        protected internal AbstractBootstrap(AbstractBootstrap<TBootstrap, TAgent> bootstrap)
        {
            this.group = bootstrap.group;
            this.agentFactory = bootstrap.agentFactory;
            this.handler = bootstrap.handler;
            this.localAddress = bootstrap.localAddress;
            this.options = new ConcurrentDictionary<AgentOption, AgentOptionValue>(bootstrap.options);
            this.attrs = new ConcurrentDictionary<IConstant, AttributeValue>(bootstrap.attrs);
        }

        /// <summary>
        /// Specifies the <see cref="IEventLoopGroup"/> which will handle events for the <see cref="IAgent"/> being built.
        /// </summary>
        /// <param name="group">The <see cref="IEventLoopGroup"/> which is used to handle all the events for the to-be-created <see cref="IAgent"/>.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public virtual TBootstrap Group(IEventLoopGroup group)
        {
            Contract.Requires(group != null);

            if (this.group != null)
            {
                throw new InvalidOperationException("group has already been set.");
            }
            this.group = group;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Specifies the <see cref="Type"/> of <see cref="IAgent"/> which will be created.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> which is used to create <see cref="IAgent"/> instances from.</typeparam>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap Agent<T>() where T : TAgent, new() => this.AgentFactory(() => new T());

        public TBootstrap AgentFactory(Func<TAgent> agentFactory)
        {
            Contract.Requires(agentFactory != null);
            this.agentFactory = agentFactory;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Assigns the <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> instance to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap LocalAddress(EndPoint localAddress)
        {
            this.localAddress = localAddress;
            return (TBootstrap)this;
        }

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// This overload binds to a <see cref="IPEndPoint"/> for any IP address on the local machine, given a specific port.
        /// </summary>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap LocalAddress(int inetPort) => this.LocalAddress(new IPEndPoint(IPAddress.Any, inetPort));

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// This overload binds to a <see cref="DnsEndPoint"/> for a given hostname and port.
        /// </summary>
        /// <param name="inetHost">The hostname to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap LocalAddress(string inetHost, int inetPort) => this.LocalAddress(new DnsEndPoint(inetHost, inetPort));

        /// <summary>
        /// Assigns the local <see cref="EndPoint"/> which is used to bind the local "end" to.
        /// This overload binds to a <see cref="IPEndPoint"/> for a given <see cref="IPAddress"/> and port.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap LocalAddress(IPAddress inetHost, int inetPort) => this.LocalAddress(new IPEndPoint(inetHost, inetPort));

        /// <summary>
        /// Allows the specification of a <see cref="AgentOption{T}"/> which is used for the
        /// <see cref="IAgent"/> instances once they get created. Use a value of <c>null</c> to remove
        /// a previously set <see cref="AgentOption{T}"/>.
        /// </summary>
        /// <param name="option">The <see cref="AgentOption{T}"/> to configure.</param>
        /// <param name="value">The value to set the given option.</param>
        public TBootstrap Option<T>(AgentOption<T> option, T value)
        {
            Contract.Requires(option != null);

            if (value == null)
            {
                AgentOptionValue removed;
                this.options.TryRemove(option, out removed);
            }
            else
            {
                this.options[option] = new AgentOptionValue<T>(option, value);
            }
            return (TBootstrap)this;
        }

        /// <summary>
        /// Allows specification of an initial attribute of the newly created <see cref="IAgent" />. If the <c>value</c> is
        /// <c>null</c>, the attribute of the specified <c>key</c> is removed.
        /// </summary>
        public TBootstrap Attribute<T>(AttributeKey<T> key, T value)
            where T : class
        {
            Contract.Requires(key != null);

            if (value == null)
            {
                AttributeValue removed;
                this.attrs.TryRemove(key, out removed);
            }
            else
            {
                this.attrs[key] = new AttributeValue<T>(key, value);
            }
            return (TBootstrap)this;
        }

        /// <summary>
        /// Validates all the parameters. Sub-classes may override this, but should call the super method in that case.
        /// </summary>
        public virtual TBootstrap Validate()
        {
            if (this.group == null)
            {
                throw new InvalidOperationException("group not set");
            }
            if (this.agentFactory == null)
            {
                throw new InvalidOperationException("Agent or AgentFactory not set");
            }
            return (TBootstrap)this;
        }

        /// <summary>
        /// Returns a deep clone of this bootstrap which has the identical configuration.  This method is useful when making
        /// multiple <see cref="IAgent"/>s with similar settings.  Please note that this method does not clone the
        /// <see cref="IEventLoopGroup"/> deeply but shallowly, making the group a shared resource.
        /// </summary>
        public abstract TBootstrap Clone();

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and registers it with an <see cref="IEventLoop"/>.
        /// </summary>
        public Task RegisterAsync()
        {
            this.Validate();
            return this.InitAndRegisterAsync();
        }

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and binds it to the endpoint specified via the <see cref="LocalAddress(EndPoint)"/> methods.
        /// </summary>
        /// <returns>The bound <see cref="IAgent"/>.</returns>
        public Task<IAgent> BindAsync()
        {
            this.Validate();
            EndPoint address = this.localAddress;
            if (address == null)
            {
                throw new InvalidOperationException("localAddress must be set beforehand.");
            }
            return this.DoBindAsync(address);
        }

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and binds it.
        /// This overload binds to a <see cref="IPEndPoint"/> for any IP address on the local machine, given a specific port.
        /// </summary>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IAgent"/>.</returns>
        public Task<IAgent> BindAsync(int inetPort) => this.BindAsync(new IPEndPoint(IPAddress.Any, inetPort));

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and binds it.
        /// This overload binds to a <see cref="DnsEndPoint"/> for a given hostname and port.
        /// </summary>
        /// <param name="inetHost">The hostname to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IAgent"/>.</returns>
        public Task<IAgent> BindAsync(string inetHost, int inetPort) => this.BindAsync(new DnsEndPoint(inetHost, inetPort));

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and binds it.
        /// This overload binds to a <see cref="IPEndPoint"/> for a given <see cref="IPAddress"/> and port.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> to bind the local "end" to.</param>
        /// <param name="inetPort">The port to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IAgent"/>.</returns>
        public Task<IAgent> BindAsync(IPAddress inetHost, int inetPort) => this.BindAsync(new IPEndPoint(inetHost, inetPort));

        /// <summary>
        /// Creates a new <see cref="IAgent"/> and binds it.
        /// </summary>
        /// <param name="localAddress">The <see cref="EndPoint"/> instance to bind the local "end" to.</param>
        /// <returns>The bound <see cref="IAgent"/>.</returns>
        public Task<IAgent> BindAsync(EndPoint localAddress)
        {
            this.Validate();
            Contract.Requires(localAddress != null);

            return this.DoBindAsync(localAddress);
        }

        async Task<IAgent> DoBindAsync(EndPoint localAddress)
        {
            IAgent Agent = await this.InitAndRegisterAsync();
            await DoBind0Async(Agent, localAddress);

            return Agent;
        }

        protected async Task<IAgent> InitAndRegisterAsync()
        {
            IAgent Agent = this.agentFactory();
            try
            {
                this.Init(Agent);
            }
            catch (Exception)
            {
                Agent.Unsafe.CloseForcibly();
                // as the Agent is not registered yet we need to force the usage of the GlobalEventExecutor
                throw;
            }

            try
            {
                await this.Group().GetNext().RegisterAsync(Agent);
            }
            catch (Exception)
            {
                if (Agent.Registered)
                {
                    try
                    {
                        await Agent.CloseAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("Failed to close Agent: " + Agent, ex);
                    }
                }
                else
                {
                    Agent.Unsafe.CloseForcibly();
                }
                throw;
            }

            // If we are here and the promise is not failed, it's one of the following cases:
            // 1) If we attempted registration from the event loop, the registration has been completed at this point.
            //    i.e. It's safe to attempt bind() or connect() now because the Agent has been registered.
            // 2) If we attempted registration from the other thread, the registration request has been successfully
            //    added to the event loop's task queue for later execution.
            //    i.e. It's safe to attempt bind() or connect() now:
            //         because bind() or connect() will be executed *after* the scheduled registration task is executed
            //         because register(), bind(), and connect() are all bound to the same thread.

            return Agent;
        }

        static Task DoBind0Async(IAgent Agent, EndPoint localAddress)
        {
            // This method is invoked before AgentRegistered() is triggered.  Give user handlers a chance to set up
            // the pipeline in its AgentRegistered() implementation.
            var promise = new TaskCompletionSource();
            Agent.EventLoop.Execute(() =>
            {
                try
                {
                    Agent.BindAsync(localAddress).LinkOutcome(promise);
                }
                catch (Exception ex)
                {
                    Agent.CloseSafe();
                    promise.TrySetException(ex);
                }
            });
            return promise.Task;
        }

        protected abstract void Init(IAgent Agent);

        /// <summary>
        /// Specifies the <see cref="IAgentHandler"/> to use for serving the requests.
        /// </summary>
        /// <param name="handler">The <see cref="IAgentHandler"/> to use for serving requests.</param>
        /// <returns>The <see cref="AbstractBootstrap{TBootstrap,TAgent}"/> instance.</returns>
        public TBootstrap Handler(IAgentHandler handler)
        {
            Contract.Requires(handler != null);
            this.handler = handler;
            return (TBootstrap)this;
        }

        protected EndPoint LocalAddress() => this.localAddress;

        protected IAgentHandler Handler() => this.handler;

        /// <summary>
        /// Returns the configured <see cref="IEventLoopGroup"/> or <c>null</c> if none is configured yet.
        /// </summary>
        public IEventLoopGroup Group() => this.group;

        protected ICollection<AgentOptionValue> Options => this.options.Values;

        protected ICollection<AttributeValue> Attributes => this.attrs.Values;

        protected static void SetAgentOptions(IAgent Agent, ICollection<AgentOptionValue> options, IInternalLogger logger)
        {
            foreach (var e in options)
            {
                SetAgentOption(Agent, e, logger);
            }
        }

        protected static void SetAgentOptions(IAgent Agent, AgentOptionValue[] options, IInternalLogger logger)
        {
            foreach (var e in options)
            {
                SetAgentOption(Agent, e, logger);
            }
        }

        protected static void SetAgentOption(IAgent Agent, AgentOptionValue option, IInternalLogger logger)
        {
            try
            {
                if (!option.Set(Agent.Configuration))
                {
                    logger.Warn("Unknown Agent option '{}' for Agent '{}'", option.Option, Agent);
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Failed to set Agent option '{}' with value '{}' for Agent '{}'", option.Option, option, Agent, ex);
            }
        }

        public override string ToString()
        {
            StringBuilder buf = new StringBuilder()
                .Append(this.GetType().Name)
                .Append('(');
            if (this.group != null)
            {
                buf.Append("group: ")
                    .Append(this.group.GetType().Name)
                    .Append(", ");
            }
            if (this.agentFactory != null)
            {
                buf.Append("agentFactory: ")
                    .Append(this.agentFactory)
                    .Append(", ");
            }
            if (this.localAddress != null)
            {
                buf.Append("localAddress: ")
                    .Append(this.localAddress)
                    .Append(", ");
            }

            if (this.options.Count > 0)
            {
                buf.Append("options: ")
                    .Append(this.options.ToDebugString())
                    .Append(", ");
            }

            if (this.attrs.Count > 0)
            {
                buf.Append("attrs: ")
                    .Append(this.attrs.ToDebugString())
                    .Append(", ");
            }

            if (this.handler != null)
            {
                buf.Append("handler: ")
                    .Append(this.handler)
                    .Append(", ");
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

        protected abstract class AgentOptionValue
        {
            public abstract AgentOption Option { get; }
            public abstract bool Set(IAgentConfiguration config);
        }

        protected sealed class AgentOptionValue<T> : AgentOptionValue
        {
            public override AgentOption Option { get; }
            readonly T value;

            public AgentOptionValue(AgentOption<T> option, T value)
            {
                this.Option = option;
                this.value = value;
            }

            public override bool Set(IAgentConfiguration config) => config.SetOption(this.Option, this.value);

            public override string ToString() => this.value.ToString();
        }

        protected abstract class AttributeValue
        {
            public abstract void Set(IAttributeMap map);
        }

        protected sealed class AttributeValue<T> : AttributeValue
            where T : class
        {
            readonly AttributeKey<T> key;
            readonly T value;

            public AttributeValue(AttributeKey<T> key, T value)
            {
                this.key = key;
                this.value = value;
            }

            public override void Set(IAttributeMap config) => config.GetAttribute(this.key).Set(this.value);
        }
    }
}
