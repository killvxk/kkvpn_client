using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace driver_installer
{
    class Operations
    {
        private const string LogFileName = "DrvierInstallLog.txt";
        private const string DriverName = "kkdrv";
        private const string DriverDesc = "kkVPN Filter Driver";

        private const int ERROR_SERVICE_ALREADY_RUNNING = 1056;
        private const int SERVICE_ALL_ACCESS = 0x1FF;
        private const int SERVICE_KERNEL_DRIVER = 0x1;
        private const int SERVICE_DEMAND_START = 0x3;
        private const int SERVICE_ERROR_NORMAL = 0x1;

        #region Unmanaged code imports
        [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, uint dwDesiredAccess);

        [DllImport("advapi32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string[] lpServiceArgVectors);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", EntryPoint = "CreateServiceW", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateService(
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
        private static extern int GetLastError();
        #endregion Unmanaged code imports

        public static Task InstallCert()
        {
            return Task.Run(() =>
                {
                    X509Certificate2 certificate = new X509Certificate2("kkdrv.cer");
                    X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);

                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                });
        }

        public static Task<bool> CopyDriverFiles()
        {
            return Task.Run<bool>(() => _CopyDriverFiles());
        }

        public static Task<bool> InstallDriver()
        {
            return Task.Run<bool>(() => _InstallDriver());
        }

        public static Task<bool> ChangeBCD(bool on)
        {
            string windir = Environment.GetEnvironmentVariable("windir");
            return Task.Run<bool>(() => RunProcess(windir + "\\sysnative\\bcdedit.exe", "-set testsigning " + (on ? "on" : "off")));
        }

        public static bool DeleteService()
        {
            RunProcess("sc", "stop kkdrv");
            return RunProcess("sc", "delete kkdrv");
        }

        public static bool _InstallDriver()
        {
            IntPtr scManagerHandle = IntPtr.Zero;
            IntPtr serviceHandle = IntPtr.Zero;
            try
            {
                scManagerHandle = OpenSCManager(null, null, 0x3F);

                if (scManagerHandle == IntPtr.Zero)
                {
                    Log("Nie powiodła się instalacja usługi sterownika! LastError: " + Marshal.GetLastWin32Error().ToString());
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
                        Log("Nie powiodła się instalacja usługi sterownika! LastError: " + Marshal.GetLastWin32Error().ToString());
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

        public static void Log(string msg)
        {
            StreamWriter logfile;

            if (!File.Exists(LogFileName))
            {
                logfile = new StreamWriter(LogFileName);
            }
            else
            {
                logfile = File.AppendText(LogFileName);
            }

            logfile.WriteLine(msg);
            logfile.Close();
        }

        private static bool _CopyDriverFiles()
        {
            if (Environment.OSVersion.Version.Major != 6)
            {
                return false;
            }

            string copyFrom = "kkdrv.sys." + Environment.OSVersion.Version.Minor.ToString();

            string windir = Environment.GetEnvironmentVariable("windir");
            if (File.Exists(windir + "\\sysnative\\drivers\\kkdrv.sys"))
            {
                File.Delete(windir + "\\sysnative\\drivers\\kkdrv.sys");
            }

            File.Copy(copyFrom, windir + "\\sysnative\\drivers\\kkdrv.sys");
            if (!File.Exists(windir + "\\sysnative\\drivers\\kkdrv.sys"))
            {
                Log("Nie powiodła się kopia plików sterownika do folderu system32!");
                return false;
            }

            return true;
        }

        private static bool RunProcess(string filename, string arguments)
        {
            ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(filename, arguments);
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                Log(process.ExitCode.ToString() + ": " + process.StandardOutput.ToString());
            }

            return (process.ExitCode == 0);
        }
    }
}
