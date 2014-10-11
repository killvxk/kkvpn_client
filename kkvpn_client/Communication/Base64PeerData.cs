using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using System.Net;
using System.IO;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class Base64PeerData
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public uint IP;
        [ProtoMember(3)]
        public int Port;
        [ProtoMember(4)]
        public byte[] PublicKey;

        public Base64PeerData() { }

        public static Base64PeerData GetBase64PeerData(string Base64String)
        {
            byte[] data = System.Convert.FromBase64String(Base64String);
            return Serializer.Deserialize<Base64PeerData>(new MemoryStream(data));
        }

        public Base64PeerData(
            string Name,
            IPEndPoint Endpoint,
            byte[] PublicKey
            )
        {
            this.Name = Name;
            this.IP = Endpoint.Address.IPToInt();
            this.Port = Endpoint.Port;
            this.PublicKey = PublicKey;
        }

        public string GetBase64EncodedData()
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize<Base64PeerData>(ms, this);
            return System.Convert.ToBase64String(ms.ToArray());
        }
    }
}
