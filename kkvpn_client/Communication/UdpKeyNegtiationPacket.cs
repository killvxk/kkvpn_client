using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class UdpKeyNegotiationPacket : CommPacket
    {
        [ProtoMember(1)]
        public byte[] KeyMaterial;

        public UdpKeyNegotiationPacket() { }

        public UdpKeyNegotiationPacket(byte[] KeyMaterial)
        {
            this.PacketID = Constants._UdpKeyNegotiationPacket;

            this.KeyMaterial = KeyMaterial;
        }
    }
}
