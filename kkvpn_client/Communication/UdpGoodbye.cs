using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client.Communication
{
    [ProtoContract]
    class UdpGoodbye : CommPacket
    {
        public UdpGoodbye() { }
    }
}
