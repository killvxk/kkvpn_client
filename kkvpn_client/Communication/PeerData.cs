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
        private DateTime LastHeartbeatAt;

        public string PeerName
        {
            get { return Name; }
        }

        public string SubnetworkIPstring
        {
            get { return (new IPAddress((long)SubnetworkIP.InvertBytes())).ToString(); }
        }

        public string StatsShort
        {
            get
            {
                if (Stats != null)
                {
                    Stats.UpdateStats();
                    return string.Format("DL: {0} KB/s UL: {1} KB/s", 
                        Stats.DLSpeed.ToString("0.00"),
                        Stats.ULSpeed.ToString("0.00"));
                }
                else
                {
                    return "";
                }
            }
        }

        public string StatsLong
        {
            get
            {
                if (Stats != null)
                {
                    Stats.UpdateStats();
                    return string.Format("DL: {0} KB/s UL: {1} KB/s" + Environment.NewLine +
                                         "Odebrano bajtów: {2} KB" + Environment.NewLine +
                                         "Wysłano bajtów: {3} KB",
                        Stats.DLSpeed.ToString("0.00"),
                        Stats.ULSpeed.ToString("0.00"),
                        ((double)Stats.DLBytes / 1024.0).ToString("0.00"),
                        ((double)Stats.ULBytes / 1024.0).ToString("0.00"));
                }
                else
                {
                    return "";
                }
            }
        }

        public PeerData() 
        {
            this.Stats = new Statistics();
            this.LastHeartbeatAt = DateTime.Now;
        }

        public PeerData(string Name, UInt32 SubnetworkIP, uint IP, int Port, byte[] PublicKey)
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

        public void Heartbeat()
        {
            this.LastHeartbeatAt = DateTime.Now;
        }

        public bool Timeout(int Timeout)
        {
            return (DateTime.Now - LastHeartbeatAt).TotalSeconds > (double)Timeout;
        }

        public override string ToString()
        {
            return Name + " (" + (new IPAddress((long)IP.InvertBytes())).ToString() + ":" + Port.ToString() + ")";
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
