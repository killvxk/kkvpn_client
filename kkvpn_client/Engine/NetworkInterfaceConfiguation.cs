using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public void AddIP(uint ip, uint mask)
        {
            if (CheckForExistingEntry(ip))
            {
                return;
            }

            DeleteIP();
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

                int retVal = AddIPAddress(
                    ip.InvertBytes(),
                    mask.InvertBytes(),
                    index,
                    ref NTEContext,
                    ref NTEInstance
                    );

                if (retVal != NO_ERROR)
                {
                    throw new InterfaceConfigurationException(
                        retVal, 
                        "Nie powiodło się ustawienie nowego adresu IP w domyślnym interfejsie! " +
                        "Upewnij się, że program został uruchomiony z uprawnieniami administratora"
                        );
                }
                SaveNTEContext(NTEContext);

                uint subnetworkAddress = ip & mask;

                Process process = new Process();
                ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                process.StartInfo = startInfo;

                startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "route";

                startInfo.Arguments = "delete " + (new IPAddress((long)subnetworkAddress.InvertBytes())).ToString();
                process.Start();
                process.WaitForExit(1000);

                startInfo.Arguments = "delete " + (new IPAddress((long)ip.InvertBytes())).ToString();
                process.Start();
                process.WaitForExit(1000);
            }
            else
            {
                throw new InterfaceConfigurationException(
                    0,
                    "Nie powiodło się ustawienie nowego adresu IP w domyślnym interfejsie!"
                    );
            }
                
        }

        public void DeleteIP()
        {
            if (NTEContext != 0)
            {
                DeleteIPAddress(NTEContext);
            }
            else
            {
                DeleteIPAddress(GetSavedNTEContext());
                SaveNTEContext(0);
            }
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
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private uint GetSavedNTEContext()
        {
            RegistryKey reg = Registry.LocalMachine.OpenSubKey("SOFTWARE\\kkVPN");
            if (reg == null)
            {
                return 0;
            }

            return uint.Parse((string)reg.GetValue("NTEContext", 0));
        }

        private void SaveNTEContext(uint value)
        {
            RegistryKey reg = Registry.LocalMachine.CreateSubKey("SOFTWARE\\kkVPN");

            reg.SetValue("NTEContext", value);
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
