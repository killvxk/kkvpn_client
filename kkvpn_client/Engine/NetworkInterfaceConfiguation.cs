using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class NetworkInterfaceConfiguation
    {
        const int NO_ERROR = 0;
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        #region Unmanaged code imports

        [DllImport("iphlpapi.dll", SetLastError=true)]
        static extern int GetIpAddrTable(
            IntPtr pIpAddrTable, 
            ref int pdwSize, 
            byte bOrder
            );

        [DllImport("iphlpapi.dll", SetLastError=true)]
        static extern int AddIPAddress(
            UInt32 Address,
            UInt32 IpMask,
            UInt32 IfIndex,
            ref UInt32 NTEContext,
            ref UInt32 NTEInstance 
            );

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern int DeleteIPAddress(
            UInt32 NTEContext
            );

        #endregion Unmanaged code imports

        UInt32 NTEContext;
        UInt32 NTEInstance;

        public NetworkInterfaceConfiguation()
        {
            NTEContext = 0;
            NTEInstance = 0;
        }

        public void AddIP(UInt32 IP)
        {
            if (CheckForExistingEntry(IP))
                return;

            int InterfaceTableSize = 0;

            IntPtr mem = IntPtr.Zero;

            if (GetIpAddrTable(mem, ref InterfaceTableSize, 0) == ERROR_INSUFFICIENT_BUFFER)
            {
                mem = Marshal.AllocHGlobal(InterfaceTableSize);
	        }

            if (GetIpAddrTable(mem, ref InterfaceTableSize, 0) == NO_ERROR)
            {
                byte[] data = new byte[InterfaceTableSize];
                Marshal.Copy(mem, data, 0, InterfaceTableSize);

                uint index = BitConverter.ToUInt32(data, 8);      // get interface number
                Marshal.FreeHGlobal(mem);

                uint hostIP = IP.InvertBytes();

                int retVal = AddIPAddress(
                    hostIP,
                    0xFFFFFF,
                    index,
                    ref NTEContext,
                    ref NTEInstance
                    );

                if (retVal != NO_ERROR)
                {
                    throw new InterfaceConfigurationException(
                        retVal, 
                        "Nie powiodło się ustawienie nowego adres IP w domyślnym interfejsie!"
                        );
                }
            }
            else
            {
                throw new InterfaceConfigurationException(
                        0,
                        "Nie powiodło się ustawienie nowego adres IP w domyślnym interfejsie!"
                        );
            }
                
        }

        public void DeleteIP()
        {
            DeleteIPAddress(NTEContext);
        }

        private bool CheckForExistingEntry(uint IP)
        {
            uint InvertedIP = IP.InvertBytes();
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    if (BitConverter.ToUInt32(ip.GetAddressBytes(), 0) == InvertedIP)
                        return true;
                }
            }

            return false;
        }
    }

    public class InterfaceConfigurationException : Exception
    {
        public int ErrorCode;

        public InterfaceConfigurationException(int errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }
    }
}
