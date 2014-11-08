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
    class CommPacket
    {
        [ProtoMember(1)]
        public byte PacketID;
        [ProtoMember(2)]
        public byte Version = Constants.CommunicationVersion;

        public CommPacket() { }
    }
}
