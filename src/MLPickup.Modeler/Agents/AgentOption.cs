using MLModeling.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MLModeling.Modeler.Agents
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Net.NetworkInformation;
    using MLModeling.Common.Utilities;

    public abstract class AgentOption : AbstractConstant<AgentOption>
    {
        class AgentOptionPool : ConstantPool
        {
            protected override IConstant NewConstant<T>(int id, string name) => new AgentOption<T>(id, name);
        }

        static readonly AgentOptionPool Pool = new AgentOptionPool();

        /// <summary>
        /// Returns the <see cref="ChannelOption"/> of the specified name.
        /// </summary>
        /// <typeparam name="T">The type of option being retrieved.</typeparam>
        /// <param name="name">The name of the desired option.</param>
        /// <returns>The matching <see cref="ChannelOption{T}"/> instance.</returns>
        public static AgentOption<T> ValueOf<T>(string name) => (AgentOption<T>)Pool.ValueOf<T>(name);

        /// <summary>
        /// Returns the <see cref="ChannelOption{T}"/> of the given pair: (<see cref="Type"/>, secondary name)
        /// </summary>
        /// <typeparam name="T">The type of option being retrieved.</typeparam>
        /// <param name="firstNameComponent">
        /// A <see cref="Type"/> whose name will be used as the first part of the desired option's name.
        /// </param>
        /// <param name="secondNameComponent">
        /// A string representing the second part of the desired option's name.
        /// </param>
        /// <returns>The matching <see cref="ChannelOption{T}"/> instance.</returns>
        public static AgentOption<T> ValueOf<T>(Type firstNameComponent, string secondNameComponent) => (AgentOption<T>)Pool.ValueOf<T>(firstNameComponent, secondNameComponent);

        /// <summary>
        /// Checks whether a given <see cref="ChannelOption"/> exists.
        /// </summary>
        /// <param name="name">The name of the <see cref="ChannelOption"/>.</param>
        /// <returns><c>true</c> if a <see cref="ChannelOption"/> exists for the given <paramref name="name"/>, otherwise <c>false</c>.</returns>
        public static bool Exists(string name) => Pool.Exists(name);

        /// <summary>
        /// Creates a new <see cref="ChannelOption"/> for the given <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The type of option to create.</typeparam>
        /// <param name="name">The name to associate with the new option.</param>
        /// <exception cref="ArgumentException">Thrown if a <see cref="ChannelOption"/> for the given <paramref name="name"/> exists.</exception>
        /// <returns>The new <see cref="ChannelOption{T}"/> instance.</returns>
        public static AgentOption<T> NewInstance<T>(string name) => (AgentOption<T>)Pool.NewInstance<T>(name);

        //public static readonly AgentOption<IByteBufferAllocator> Allocator = ValueOf<IByteBufferAllocator>("ALLOCATOR");
        //public static readonly AgentOption<IRecvByteBufAllocator> RcvbufAllocator = ValueOf<IRecvByteBufAllocator>("RCVBUF_ALLOCATOR");
        //public static readonly AgentOption<IMessageSizeEstimator> MessageSizeEstimator = ValueOf<IMessageSizeEstimator>("MESSAGE_SIZE_ESTIMATOR");

        public static readonly AgentOption<TimeSpan> ConnectTimeout = ValueOf<TimeSpan>("CONNECT_TIMEOUT");
        public static readonly AgentOption<int> WriteSpinCount = ValueOf<int>("WRITE_SPIN_COUNT");
        public static readonly AgentOption<int> WriteBufferHighWaterMark = ValueOf<int>("WRITE_BUFFER_HIGH_WATER_MARK");
        public static readonly AgentOption<int> WriteBufferLowWaterMark = ValueOf<int>("WRITE_BUFFER_LOW_WATER_MARK");

        public static readonly AgentOption<bool> AllowHalfClosure = ValueOf<bool>("ALLOW_HALF_CLOSURE");
        public static readonly AgentOption<bool> AutoRead = ValueOf<bool>("AUTO_READ");

        public static readonly AgentOption<bool> SoBroadcast = ValueOf<bool>("SO_BROADCAST");
        public static readonly AgentOption<bool> SoKeepalive = ValueOf<bool>("SO_KEEPALIVE");
        public static readonly AgentOption<int> SoSndbuf = ValueOf<int>("SO_SNDBUF");
        public static readonly AgentOption<int> SoRcvbuf = ValueOf<int>("SO_RCVBUF");
        public static readonly AgentOption<bool> SoReuseaddr = ValueOf<bool>("SO_REUSEADDR");
        public static readonly AgentOption<bool> SoReuseport = ValueOf<bool>("SO_REUSEPORT");
        public static readonly AgentOption<int> SoLinger = ValueOf<int>("SO_LINGER");
        public static readonly AgentOption<int> SoBacklog = ValueOf<int>("SO_BACKLOG");
        public static readonly AgentOption<int> SoTimeout = ValueOf<int>("SO_TIMEOUT");

        public static readonly AgentOption<int> IpTos = ValueOf<int>("IP_TOS");
        public static readonly AgentOption<EndPoint> IpMulticastAddr = ValueOf<EndPoint>("IP_MULTICAST_ADDR");
        public static readonly AgentOption<NetworkInterface> IpMulticastIf = ValueOf<NetworkInterface>("IP_MULTICAST_IF");
        public static readonly AgentOption<int> IpMulticastTtl = ValueOf<int>("IP_MULTICAST_TTL");
        public static readonly AgentOption<bool> IpMulticastLoopDisabled = ValueOf<bool>("IP_MULTICAST_LOOP_DISABLED");

        public static readonly AgentOption<bool> TcpNodelay = ValueOf<bool>("TCP_NODELAY");

        internal AgentOption(int id, string name)
            : base(id, name)
        {
        }

        public abstract bool Set(IAgentConfiguration configuration, object value);
    }

    public sealed class AgentOption<T> : AgentOption
    {
        internal AgentOption(int id, string name)
            : base(id, name)
        {
        }

        public void Validate(T value) => Contract.Requires(value != null);

        public override bool Set(IAgentConfiguration configuration, object value) => configuration.SetOption(this, (T)value);
    }
}
