using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Net;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class PeerData
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public uint SubnetworkIP;
        [ProtoMember(3)]
        public uint IP;
        [ProtoMember(4)]
        public int Port;
        [ProtoMember(5)]
        public byte[] PublicKey;

        public int KeyIndex;
        //public bool KeyExchangeInProgress;

        public PeerData() { }

        public PeerData(
            string Name,
            UInt32 SubnetworkIP,
            uint IP,
            int Port,
            byte[] PublicKey
            )
        {
            this.Name = Name;
            this.SubnetworkIP = SubnetworkIP;
            this.IP = IP;
            this.Port = Port;
            this.PublicKey = PublicKey;
            //this.KeyExchangeInProgress = false;

            this.KeyIndex = 0;
        }

        public IPEndPoint GetEndpoint()
        {
            return new IPEndPoint((long)IP.InvertBytes(), Port);
        }

        internal static PeerData GetDataFromBase64PeerData(Base64PeerData peerData, uint SubnetworkIP)
        {
            return new PeerData(
                peerData.Name, 
                SubnetworkIP, 
                peerData.IP, 
                peerData.Port, 
                peerData.PublicKey
                );
        }
    }
}
