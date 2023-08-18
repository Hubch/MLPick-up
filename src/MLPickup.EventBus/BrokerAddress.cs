using System;
using System.Net;
using System.Net.Sockets;

namespace MLPickup.EventBus
{
    public class BrokerAddress
    {
        private int _hash;
        internal  byte[] Buffer;
        internal bool _changed = true;
        internal int InternalSize;
        public BrokerAddress(AddressProperty property)
        {
        }
        public BrokerAddress(string ip, int port)
        {
        }

        public BrokerAddress(AddressProperty property, int size)
        {
        }

        public AddressProperty Property
        {
            get
            {
                throw null;
            }
        }

        public override bool Equals(object comparand)
        {
            throw null;
        }

        public override int GetHashCode()
        {
            throw null;
        }

        public override string ToString()
        {
            throw null;
        }
    }

}
