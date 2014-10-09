using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    public class UdpNewPeerPacket : CommPacket
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public uint SubnetworkIP;
        [ProtoMember(3)]
        public bool RecipiantIsNew;
        [ProtoMember(4)]
        public PeerData[] Peers;
        [ProtoMember(5)]
        public Subnetwork SubnetworkData;
        [ProtoMember(6)]
        public IPEndPoint Endpoint;

        public UdpNewPeerPacket() { }
    }
}
