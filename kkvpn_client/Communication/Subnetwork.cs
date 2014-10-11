using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    [ProtoContract]
    class Subnetwork
    {
        [ProtoMember(1)]
        public string Name;
        [ProtoMember(2)]
        public uint Address;
        [ProtoMember(3)]
        public uint CIDR;
    }
}
