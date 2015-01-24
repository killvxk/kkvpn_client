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
    class UdpNewPeerPacket : CommPacket
    {
        [ProtoMember(1)]
        public string Name;                     // nazwa użytkownika
        [ProtoMember(2)]
        public uint SubnetworkIP;               // adres IP w sieci wirtualnej
        [ProtoMember(3)]
        public bool RecipientIsNew;             // flaga, true dla dołączającego węzła
        [ProtoMember(4)]
        public PeerData[] Peers;                // tablica z danymi węzłów
        [ProtoMember(5)]
        public Subnetwork SubnetworkData;       // dane sieci wirtualnej
        [ProtoMember(6)]
        public uint PreferedCipher;             // zarezerwowane

        public UdpNewPeerPacket()
        {

        }

        public UdpNewPeerPacket(
            string Name,
            uint SubnetworkIP,
            bool RecipiantIsNew,
            PeerData[] Peers,
            Subnetwork SubnetworkData
            ) 
        {
            this.Name = Name;
            this.SubnetworkIP = SubnetworkIP;
            this.RecipientIsNew = RecipiantIsNew;
            this.Peers = Peers;
            this.SubnetworkData = SubnetworkData;
            this.PreferedCipher = 0;
        }
    }
}
