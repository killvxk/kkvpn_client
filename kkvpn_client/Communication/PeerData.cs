using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Net;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    public class PeerData
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public uint SubnetworkIP;
        [ProtoMember(3)]
        public uint IP;
        [ProtoMember(4)]
        public uint Port;
        [ProtoMember(5)]
        public byte[] PublicKey;

        public int AesKey;
        public bool KeyExchangeInProgress;

        public PeerData() { }

        public PeerData(
            string Name,
            UInt32 SubnetworkIP,
            UInt32 IP,
            UInt16 Port,
            byte[] PublicKey
            )
        {
            this.Name = Name;
            this.SubnetworkIP = SubnetworkIP;
            this.IP = IP;
            this.Port = Port;
            this.PublicKey = PublicKey;
            this.KeyExchangeInProgress = false;

            this.AesKey = 0;
        }

        public IPEndPoint GetEndpoint() 
        {
            return new IPEndPoint((long)IP, (int)Port);
        }
    }
}
