using kkvpn_client.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace kkvpn_client
{
    class DriverSystemCheckAndStart
    {
        const string DriverName = "kkdrv";
        const string DriverDesc = "kkVPN Filter Driver";

        const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
        const int SERVICE_ALL_ACCESS = 0x1FF;
        const int SERVICE_KERNEL_DRIVER = 0x1;
        const int SERVICE_DEMAND_START = 0x3;
        const int SERVICE_ERROR_NORMAL = 0x1;

        #region Unmanaged code imports
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", EntryPoint = "CreateServiceW", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr CreateService(
            IntPtr hSCManager,
            string lpServiceName,
            string lpDisplayName,
            uint dwDesiredAccess,
            uint dwServiceType,
            uint dwStartType,
            uint dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            string lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
            );

        [DllImport("kernel32.dll")]
        static extern int GetLastError();
        #endregion Unmanaged code imports
        
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

        public static bool StartDriver()
        {
            IntPtr scManagerHandle = IntPtr.Zero;
            IntPtr serviceHandle = IntPtr.Zero;
            try
            {
                scManagerHandle = OpenSCManager(null, null, 0x3F);

                if (scManagerHandle == IntPtr.Zero)
                {
                    return false;
                }

                serviceHandle = OpenService(scManagerHandle, DriverName, SERVICE_ALL_ACCESS);

                if (serviceHandle == IntPtr.Zero)
                {
                    serviceHandle = CreateService(scManagerHandle,
                                        DriverName,
                                        DriverDesc,
                                        SERVICE_ALL_ACCESS,
                                        SERVICE_KERNEL_DRIVER,
                                        SERVICE_DEMAND_START,
                                        SERVICE_ERROR_NORMAL,
                                        Environment.SystemDirectory + "\\drivers\\kkdrv.sys",
                                        null,
                                        null,
                                        null,
                                        null,
                                        null
                                        );
                    if (serviceHandle == IntPtr.Zero)
                    {
                        return false;
                    }
                }

                if (!StartService(serviceHandle, 0, null))
                {
                    if (Marshal.GetLastWin32Error() != ERROR_SERVICE_ALREADY_RUNNING)
                    {
                        Logger.Instance.LogError("Nie powiodło się uruchomienie usługi sterownika! LastError: " + Marshal.GetLastWin32Error().ToString());
                        return false;
                    }
                }

                return true;
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
