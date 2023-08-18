using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLPickup.Modeler.Bootstrapping
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using MLPickup.Common.Internal.Logging;
    using MLPickup.Modeler.Agents;
    using TaskCompletionSource = MLPickup.Common.Concurrency.TaskCompletionSource;

    /// <summary>
    /// A <see cref="Bootstrap"/> that makes it easy to bootstrap an <see cref="IAgent"/> to use for clients.
    /// 
    /// The <see cref="AbstractBootstrap{TBootstrap,TAgent}.BindAsync(EndPoint)"/> methods are useful
    /// in combination with connectionless transports such as datagram (UDP). For regular TCP connections,
    /// please use the provided <see cref="ConnectAsync(EndPoint,EndPoint)"/> methods.
    /// </summary>
    public class Bootstrap : AbstractBootstrap<Bootstrap, IAgent>
    {
        static readonly IInternalLogger Logger = InternalLoggerFactory.GetInstance<Bootstrap>();

        static readonly INameResolver DefaultResolver = new DefaultNameResolver();

        volatile INameResolver resolver = DefaultResolver;
        volatile EndPoint remoteAddress;

        public Bootstrap()
        {
        }

        Bootstrap(Bootstrap bootstrap)
            : base(bootstrap)
        {
            this.resolver = bootstrap.resolver;
            this.remoteAddress = bootstrap.remoteAddress;
        }

        /// <summary>
        /// Sets the <see cref="INameResolver"/> which will resolve the address of the unresolved named address.
        /// </summary>
        /// <param name="resolver">The <see cref="INameResolver"/> which will resolve the address of the unresolved named address.</param>
        /// <returns>The <see cref="Bootstrap"/> instance.</returns>
        public Bootstrap Resolver(INameResolver resolver)
        {
            Contract.Requires(resolver != null);
            this.resolver = resolver;
            return this;
        }

        /// <summary>
        /// Assigns the remote <see cref="EndPoint"/> to connect to once the <see cref="ConnectAsync()"/> method is called.
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <returns>The <see cref="Bootstrap"/> instance.</returns>
        public Bootstrap RemoteAddress(EndPoint remoteAddress)
        {
            this.remoteAddress = remoteAddress;
            return this;
        }

        /// <summary>
        /// Assigns the remote <see cref="EndPoint"/> to connect to once the <see cref="ConnectAsync()"/> method is called.
        /// </summary>
        /// <param name="inetHost">The hostname of the endpoint to connect to.</param>
        /// <param name="inetPort">The port at the remote host to connect to.</param>
        /// <returns>The <see cref="Bootstrap"/> instance.</returns>
        public Bootstrap RemoteAddress(string inetHost, int inetPort)
        {
            this.remoteAddress = new DnsEndPoint(inetHost, inetPort);
            return this;
        }

        /// <summary>
        /// Assigns the remote <see cref="EndPoint"/> to connect to once the <see cref="ConnectAsync()"/> method is called.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> of the endpoint to connect to.</param>
        /// <param name="inetPort">The port at the remote host to connect to.</param>
        /// <returns>The <see cref="Bootstrap"/> instance.</returns>
        public Bootstrap RemoteAddress(IPAddress inetHost, int inetPort)
        {
            this.remoteAddress = new IPEndPoint(inetHost, inetPort);
            return this;
        }

        /// <summary>
        /// Connects an <see cref="IAgent"/> to the remote peer.
        /// </summary>
        /// <returns>The <see cref="IAgent"/>.</returns>
        public Task<IAgent> ConnectAsync()
        {
            this.Validate();
            EndPoint remoteAddress = this.remoteAddress;
            if (remoteAddress == null)
            {
                throw new InvalidOperationException("remoteAddress not set");
            }

            return this.DoResolveAndConnectAsync(remoteAddress, this.LocalAddress());
        }

        /// <summary>
        /// Connects an <see cref="IAgent"/> to the remote peer.
        /// </summary>
        /// <param name="inetHost">The hostname of the endpoint to connect to.</param>
        /// <param name="inetPort">The port at the remote host to connect to.</param>
        /// <returns>The <see cref="IAgent"/>.</returns>
        public Task<IAgent> ConnectAsync(string inetHost, int inetPort) => this.ConnectAsync(new DnsEndPoint(inetHost, inetPort));

        /// <summary>
        /// Connects an <see cref="IAgent"/> to the remote peer.
        /// </summary>
        /// <param name="inetHost">The <see cref="IPAddress"/> of the endpoint to connect to.</param>
        /// <param name="inetPort">The port at the remote host to connect to.</param>
        /// <returns>The <see cref="IAgent"/>.</returns>
        public Task<IAgent> ConnectAsync(IPAddress inetHost, int inetPort) => this.ConnectAsync(new IPEndPoint(inetHost, inetPort));

        /// <summary>
        /// Connects an <see cref="IAgent"/> to the remote peer.
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <returns>The <see cref="IAgent"/>.</returns>
        public Task<IAgent> ConnectAsync(EndPoint remoteAddress)
        {
            Contract.Requires(remoteAddress != null);

            this.Validate();
            return this.DoResolveAndConnectAsync(remoteAddress, this.LocalAddress());
        }

        /// <summary>
        /// Connects an <see cref="IAgent"/> to the remote peer.
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <param name="localAddress">The local <see cref="EndPoint"/> to connect to.</param>
        /// <returns>The <see cref="IAgent"/>.</returns>
        public Task<IAgent> ConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            Contract.Requires(remoteAddress != null);

            this.Validate();
            return this.DoResolveAndConnectAsync(remoteAddress, localAddress);
        }

        /// <summary>
        /// Performs DNS resolution for the remote endpoint and connects to it.
        /// </summary>
        /// <param name="remoteAddress">The remote <see cref="EndPoint"/> to connect to.</param>
        /// <param name="localAddress">The local <see cref="EndPoint"/> to connect the remote to.</param>
        /// <returns>The <see cref="IAgent"/>.</returns>
        async Task<IAgent> DoResolveAndConnectAsync(EndPoint remoteAddress, EndPoint localAddress)
        {
            IAgent Agent = await this.InitAndRegisterAsync();

            if (this.resolver.IsResolved(remoteAddress))
            {
                // Resolver has no idea about what to do with the specified remote address or it's resolved already.
                await DoConnectAsync(Agent, remoteAddress, localAddress);
                return Agent;
            }

            EndPoint resolvedAddress;
            try
            {
                resolvedAddress = await this.resolver.ResolveAsync(remoteAddress);
            }
            catch (Exception)
            {
                try
                {
                    await Agent.CloseAsync();
                }
                catch (Exception ex)
                {
                    Logger.Warn("Failed to close Agent: " + Agent, ex);
                }

                throw;
            }

            await DoConnectAsync(Agent, resolvedAddress, localAddress);
            return Agent;
        }

        static Task DoConnectAsync(IAgent Agent,
            EndPoint remoteAddress, EndPoint localAddress)
        {
            // This method is invoked before AgentRegistered() is triggered.  Give user handlers a chance to set up
            // the pipeline in its AgentRegistered() implementation.
            var promise = new TaskCompletionSource();
            Agent.EventLoop.Execute(() =>
            {
                try
                {
                    if (localAddress == null)
                    {
                        Agent.ConnectAsync(remoteAddress).LinkOutcome(promise);
                    }
                    else
                    {
                        Agent.ConnectAsync(remoteAddress, localAddress).LinkOutcome(promise);
                    }
                }
                catch (Exception ex)
                {
                    Agent.CloseSafe();
                    promise.TrySetException(ex);
                }
            });
            return promise.Task;
        }

        protected override void Init(IAgent Agent)
        {
            IAgentPipeline p = Agent.Pipeline;
            p.AddLast(null, (string)null, this.Handler());

            ICollection<AgentOptionValue> options = this.Options;
            SetAgentOptions(Agent, options, Logger);

            ICollection<AttributeValue> attrs = this.Attributes;
            foreach (AttributeValue e in attrs)
            {
                e.Set(Agent);
            }
        }

        public override Bootstrap Validate()
        {
            base.Validate();
            if (this.Handler() == null)
            {
                throw new InvalidOperationException("handler not set");
            }
            return this;
        }

        public override Bootstrap Clone() => new Bootstrap(this);

        /// <summary>
        /// Returns a deep clone of this bootstrap which has the identical configuration except that it uses
        /// the given <see cref="IEventLoopGroup"/>. This method is useful when making multiple <see cref="IAgent"/>s with similar
        /// settings.
        /// </summary>
        public Bootstrap Clone(IEventLoopGroup group)
        {
            var bs = new Bootstrap(this);
            bs.Group(group);
            return bs;
        }

        public override string ToString()
        {
            if (this.remoteAddress == null)
            {
                return base.ToString();
            }

            var buf = new StringBuilder(base.ToString());
            buf.Length = buf.Length - 1;

            return buf.Append(", remoteAddress: ")
                .Append(this.remoteAddress)
                .Append(')')
                .ToString();
        }
    }
}
