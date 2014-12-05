using kkvpn_client.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class DriverSystemCheckAndStart
    {
        [DllImport("advapi32.dll", EntryPoint="OpenSCManagerW", ExactSpelling=true, CharSet=CharSet.Unicode, SetLastError=true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);


        public static bool CheckStatus(string Device)
        {
            ManagementObjectSearcher deviceList =
                new ManagementObjectSearcher("Select HardwareID, Status from Win32_PnPEntity where DeviceID like '%VPNDRIVER%'");

            if (deviceList != null)
                foreach (ManagementObject device in deviceList.Get())
                {
                    string status = (string)device.GetPropertyValue("Status");
                    if ((status != "OK") && (status != "Degraded")
                        && (status != "Pred Fail"))
                        continue;

                    string[] HardwareIDs = (string[])device.GetPropertyValue("HardwareID");

                    foreach (string HardwareID in HardwareIDs)
                        if (HardwareID == Device)
                            return true;
                }

            return false;
        }

        public static bool StartDriver(string ServiceName)
        {
            IntPtr scManagerHandle = IntPtr.Zero;
            IntPtr serviceHandle = IntPtr.Zero;
            try
            {
                scManagerHandle = OpenSCManager(null, null, 0x30);   // SERVICE_START || SERVICE_STOP

                if (scManagerHandle == IntPtr.Zero)
                {
                    return false;
                }

                serviceHandle = OpenService(scManagerHandle, ServiceName, 0x30);   // SERVICE_ALL_ACCESS = 0xF01FF

                if (serviceHandle == IntPtr.Zero)
                {
                    return false;
                }

                return StartService(serviceHandle, 0, null);
            }
            finally
            {
                if (serviceHandle != IntPtr.Zero)
                {
                    CloseServiceHandle(serviceHandle);
                }
                if (scManagerHandle != IntPtr.Zero)
                {
                    CloseServiceHandle(scManagerHandle);
                }
            }
        }
    }

    
}
