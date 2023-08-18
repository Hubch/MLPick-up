
using System.Net.Sockets;
using System.Net;
using System;

namespace MLModeling.EventBus
{
    /// <summary>
    /// 标识消息地址
    /// </summary>
    public abstract class BrokerPoint
    {
        protected BrokerPoint()
        {

        }
   
        public virtual AddressProperty AddressProperty { get; }

        public virtual BrokerPoint Create(BrokerAddress brokerAddress)
        {
            throw null;
        }

        public virtual BrokerAddress Serialize()
        {
            throw null;
        }
    }
}
