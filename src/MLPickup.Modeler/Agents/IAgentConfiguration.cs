

namespace MLPickup.Modeler.Agents
{
    using System;
    public interface IAgentConfiguration
    {
        T GetOption<T>(AgentOption<T> option);

        bool SetOption(AgentOption option, object value);

        bool SetOption<T>(AgentOption<T> option, T value);

        TimeSpan ConnectTimeout { get; set; }

        int WriteSpinCount { get; set; }

        //TODO 暂时先注释
        //IByteBufferAllocator Allocator { get; set; }

        //IRecvByteBufAllocator RecvByteBufAllocator { get; set; }

        bool AutoRead { get; set; }

        int WriteBufferHighWaterMark { get; set; }

        int WriteBufferLowWaterMark { get; set; }

        //IMessageSizeEstimator MessageSizeEstimator { get; set; }
    }
}
