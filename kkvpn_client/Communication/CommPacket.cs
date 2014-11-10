using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    [ProtoInclude(101, typeof(UdpEncryptedPacket))]
    [ProtoInclude(102, typeof(UdpKeyNegotiationPacket))]
    [ProtoInclude(103, typeof(UdpNewPeerPacket))]
    [ProtoInclude(104, typeof(UdpConnectingConfirmation))]
    [ProtoInclude(105, typeof(UdpHeartbeat))]
    [ProtoInclude(105, typeof(UdpGoodbye))]
    class CommPacket
    {
        [ProtoMember(1)]
        public byte PacketID;
        [ProtoMember(2)]
        public byte Version = Constants.CommunicationVersion;

        public CommPacket() { }
    }
}
