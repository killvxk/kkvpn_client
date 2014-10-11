using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    static class MiscFunctions
    {
        public static uint GetMaskFromCIDR(this uint CIDR)
        {
            int result = 0;
            for (int i = 0; i < CIDR; i++)
                result += (1 << i);

            result <<= (int)(32 - CIDR);

            return (uint)result;
        }

        public static uint InvertBytes(this uint Data)
        {
            byte[] Temp = BitConverter.GetBytes(Data);
            Array.Reverse(Temp);
            return BitConverter.ToUInt32(Temp, 0);
        }

        public static ushort InvertBytes(this ushort Data)
        {
            return (UInt16)((Data >> 8) | ((Data & 0xFF) << 8));
        }

        public static uint IPToInt(this string Host)
        {
            IPAddress IP;
            if (IPAddress.TryParse(Host, out IP))
            {
                byte[] temp = IP.GetAddressBytes();
                Array.Reverse(temp);
                return BitConverter.ToUInt32(temp, 0);
            }
            else
                return 0;
        }

        public static uint IPToInt(this IPAddress Host)
        {
            byte[] temp = Host.GetAddressBytes();
            Array.Reverse(temp);
            return BitConverter.ToUInt32(temp, 0);
        }
    }
}
