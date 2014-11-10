using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class UdpEncryptedPacket : CommPacket
    {
        [ProtoMember(1)]
        public byte KeyID;
        [ProtoMember(2)]
        public EncryptedData Data;

        public UdpEncryptedPacket() { }

        public UdpEncryptedPacket(byte KeyID, EncryptedData Data)
        {
            this.PacketID = Constants._UdpEncryptedPacket;

            this.KeyID = KeyID;
            this.Data = Data;
        }
    }
}
