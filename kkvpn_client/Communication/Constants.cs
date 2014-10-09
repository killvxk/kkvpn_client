using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace kkvpn_client.Communication
{
    public static class Constants
    {
        public const byte CommunicationVersion = 1;

        #region Packet IDs

        public const byte _UdpEncryptedPacket = 1;
        public const byte _UdpKeyNegotiationPacket = 2;
        public const byte _UdpNewPeerPacket = 3;

        #endregion Packet IDs
    }
}
