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
        [ProtoMember(2)]
        public byte[] DSASignature;

        public UdpKeyNegotiationPacket() { }

        public UdpKeyNegotiationPacket(byte[] KeyMaterial, byte[] DSASignature)
        {
            this.KeyMaterial = KeyMaterial;
            this.DSASignature = DSASignature;
        }
    }
}
