using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    [ProtoInclude(101, typeof(UdpKeyNegotiationPacket))]
    [ProtoInclude(102, typeof(UdpNewPeerPacket))]
    [ProtoInclude(103, typeof(UdpConnectingConfirmation))]
    [ProtoInclude(104, typeof(UdpHeartbeat))]
    [ProtoInclude(105, typeof(UdpGoodbye))]
    class CommPacket
    {
        [ProtoMember(1)]
        public byte Version = Constants.CommunicationVersion;

        public CommPacket() { }
    }
}
