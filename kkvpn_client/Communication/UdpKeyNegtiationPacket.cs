using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    public class UdpKeyNegotiationPacket : CommPacket
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public byte[] KeyMaterial;

        public UdpKeyNegotiationPacket() { }

        public UdpKeyNegotiationPacket(string Name, byte[] KeyMaterial)
        {
            this.PacketID = Constants._UdpKeyNegotiationPacket;

            this.Name = Name;
            this.KeyMaterial = KeyMaterial;
        }
    }
}
