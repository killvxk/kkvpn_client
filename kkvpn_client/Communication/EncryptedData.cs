using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class EncryptedData
    {
        [ProtoMember(1)]
        public byte[] Data;
        [ProtoMember(2)]
        public byte[] IV;
        [ProtoMember(3)]
        public ushort DataLength;

        public EncryptedData() { }

        public EncryptedData(byte[] EncryptedData, byte[] IV, ushort DataLength)
        {
            this.Data = EncryptedData;
            this.IV = IV;
            this.DataLength = DataLength;
        }
    }
}
