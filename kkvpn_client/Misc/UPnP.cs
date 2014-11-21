using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Nat;
using System.Threading;
using System.Net;

namespace kkvpn_client
{
    class UPnPPortMapper : IDisposable
    {
        private List<INatDevice> devices;
        private Thread DiscoveryThread;

        public UPnPPortMapper()
        {
            devices = new List<INatDevice>();
            NatUtility.DeviceFound += NatUtility_DeviceFound;
            NatUtility.DeviceLost += NatUtility_DeviceLost;

            DiscoveryThread = new Thread(() => 
            {
                while(true)
                {
                    try
                    {
                        NatUtility.StartDiscovery();
                        Thread.Sleep(5 * 60 * 1000);        //co 5 minut
                        NatUtility.StopDiscovery();
                    }
                    catch (ThreadAbortException)
                    { }
                }
            });
            DiscoveryThread.IsBackground = true;
            DiscoveryThread.Start();
        }

        public bool MapPort(int local, int remote)
        {
            bool result = false;

            foreach (INatDevice device in devices)
            {
                try
                {
                    device.CreatePortMap(new Mapping(Protocol.Udp, local, remote));
                    result = true;
                }
                catch (MappingException)
                { }
            }

            return result;
        }

        public IPAddress GetExternalIP()
        {
            foreach (INatDevice device in devices)
            {
                if (device.GetExternalIP() != null)
                {
                    return device.GetExternalIP();
                }
            }

            return null;
        }

        private void NatUtility_DeviceFound(object sender, DeviceEventArgs e)
        {
            devices.Add(e.Device);
        }

        private void NatUtility_DeviceLost(object sender, DeviceEventArgs e)
        {
            devices.Remove(e.Device);
        }

        public void Dispose()
        {
            DiscoveryThread.Abort();
        }
    }
}
