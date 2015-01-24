using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class UdpConnectingConfirmation : CommPacket
    {
        public UdpConnectingConfirmation() { }
    }
}
