using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.Net;
using System.Net.Sockets;

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

        public int? KeyIndex;
        public Statistics Stats;
        public bool KeyExchangeInProgress;

        public string PeerName
        {
            get { return Name; }
        }

        public string SubnetworkIPstring
        {
            get { return (new IPAddress((long)SubnetworkIP.InvertBytes())).ToString(); }
        }

        public PeerData() 
        {
            this.Stats = new Statistics();
        }

        public PeerData(
            string Name,
            UInt32 SubnetworkIP,
            uint IP,
            int Port,
            byte[] PublicKey
            )
            : this()
        {
            this.Name = Name;
            this.SubnetworkIP = SubnetworkIP;
            this.IP = IP;
            this.Port = Port;
            this.PublicKey = PublicKey;
            this.KeyExchangeInProgress = false;

            this.KeyIndex = null;
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
