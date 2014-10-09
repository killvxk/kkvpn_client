using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace kkvpn_client
{
    class DriverSystemCheck
    {
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
    }
}
