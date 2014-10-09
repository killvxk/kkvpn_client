using kkvpn_client.Communication;
using kkvpn_client.Engine;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace kkvpn_client
{
    public class ConnectionManager
    {
        private const int UdpPort = 57384;

        private Subnetwork CurrentSubnetwork;
        private uint IP;
        private UdpClient Udp;
        private bool _Connected;

        private IEncryptionEngine Encryption;
        private DriverConnector Driver;
        private NetworkInterfaceConfiguation InterfaceConfig;

        private Dictionary<UInt32, PeerData> PeersTx;
        private Dictionary<IPEndPoint, PeerData> PeersRx;

        private int LastSpeedCheck;
        private ulong LastSpeedCheckDL;
        private ulong LastSpeedCheckUL;

        private Statistics Stats;
        private bool _PortForwarded;
        private bool AwaitingExternalConnection;
        private bool DriverStarted;

        private CancellationTokenSource cancelTokenSource;

        public bool Connected
        {
            get { return _Connected; }
        }

        public bool PortForwarded
        {
            get { return _PortForwarded; }
        }

        public ConnectionManager()
        {
            Stats = new Statistics();

            PeersTx = new Dictionary<uint, PeerData>();
            PeersRx = new Dictionary<IPEndPoint, PeerData>();

            cancelTokenSource = new CancellationTokenSource();

            AwaitingExternalConnection = false;
            DriverStarted = false;
            _Connected = false;
        }

        public void InitializeManager()
        {
            Driver = new DriverConnector();
            Driver.InitializeDevice();
            InterfaceConfig = new NetworkInterfaceConfiguation();
        }

        public Task CreateNewNetwork(string NetworkName, string UserName, uint Address, uint CIDR)
        {
            return Task.Run(() => 
            {
                if (_Connected)
                {
                    throw new InvalidOperationException("Obecnie jest już otwarte jedno połączenie.");
                }

                try
                {
                    CurrentSubnetwork = new Subnetwork() 
                    { 
                        Address = Address, 
                        Mask = CIDR, 
                        Name = NetworkName 
                    };
                    IP = Address + 1;           // pierwszy dostępny adres

                    StartNetworkEngine();
                    StartDriver(CurrentSubnetwork, IP);
                }
                catch (OperationCanceledException) { }
                catch
                {
                    throw;
                }
            }, cancelTokenSource.Token);
        }

        public Task OpenForConnection()
        {
            return Task.Run(() => 
            {
                if (_Connected)
                {
                    throw new InvalidOperationException("Obecnie jest już otwarte jedno połączenie.");
                }

                try
                {
                    StartNetworkEngine();
                    AwaitingExternalConnection = true;
                }
                catch (OperationCanceledException) { }
                catch
                {
                    throw;
                }
            }, cancelTokenSource.Token);
        }

        public void CancelCurrentOperation()
        {
            cancelTokenSource.Cancel();
        }

        public string GetConnectionString(string UserName)
        {
            return "";
        }

        public Dictionary<string, string> GetSubnetworkInfo()
        {
            if (CurrentSubnetwork == null)
                return null;
            else
                return new Dictionary<string, string>()
                {
                    {"NetworkName", CurrentSubnetwork.Name},
                    {"Subnetwork", (new IPAddress(CurrentSubnetwork.Address.InvertBytes())).ToString()},
                    {"Mask", (new IPAddress(CurrentSubnetwork.Mask.GetMaskFromCIDR().InvertBytes())).ToString()},
                    {"IP", (new IPAddress(IP.InvertBytes())).ToString()}
                };
        }

        public Statistics GetOverallStatistics()
        {
            double time = Environment.TickCount - LastSpeedCheck;
            LastSpeedCheck = Environment.TickCount;

            Stats.DLSpeed = (double)(Stats.DLBytes - LastSpeedCheckDL) / time;
            Stats.ULSpeed = (double)(Stats.ULBytes - LastSpeedCheckUL) / time;

            LastSpeedCheckDL = Stats.DLBytes;
            LastSpeedCheckUL = Stats.ULBytes;

            return Stats;
        }

        public Statistics GetPeerStatistics(int index)
        {
            Statistics stats = new Statistics();

            return stats;
        }

        public void AddPeer(string ConnectionString)
        {
            if (!_Connected)
            {
                throw new InvalidOperationException("Żadne połączenie nie jest obecnie otwarte.");
            }
        }

        public void Disconnect()
        {
            if (!_Connected)
            {
                throw new InvalidOperationException("Żadne połączenie nie jest obecnie otwarte.");
            }

            StopDriver();

            _Connected = false;
        }

        private void StartNetworkEngine()
        {
            try
            {
                UPnP.NAT.Discover();
                UPnP.NAT.ForwardPort(UdpPort, ProtocolType.Udp, "kkVPN port");
                _PortForwarded = true;
            }
            catch (WebException)
            {
                _PortForwarded = false;
            }
            
            Udp = new UdpClient(new IPEndPoint(IPAddress.Any, UdpPort));

            Thread RxThread = new Thread(RxWorkerRoutine);
            RxThread.Start();

            _Connected = true;
        }

        private void StartDriver(Subnetwork subnetwork, uint localIP)
        {
            Driver.SetFilter(subnetwork.Address, subnetwork.Mask, localIP);
            InterfaceConfig.AddIP(localIP);
            Driver.StartReading(ProcessReceivedData);

            DriverStarted = true;
        }

        private void StopDriver()
        {
            if (DriverStarted)
            {
                Driver.StopReading();
                Driver.ResetFilter();
                InterfaceConfig.DeleteIP();
            }

            DriverStarted = false;
        }

        private void RxWorkerRoutine()
        {
            Udp.Client.ReceiveTimeout = 1000;
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            while (_Connected)
            {
                try
                {
                    byte[] data = Udp.Receive(ref ep);

                    if (data == null || data.Length == 0)
                    {
                        return;
                    }

                    ProcessUdpPacket(Serializer.Deserialize<CommPacket>(new MemoryStream(data)), ep);
                }
                catch (SocketException ex)
                {
                    if (ex.ErrorCode != 10060)
                    {
                        throw;
                    }
                }
            }

            Udp.Close();
        }

        private void ProcessUdpPacket(CommPacket packet, IPEndPoint ep)
        {
            PeerData peer = null;

            if (packet is UdpEncryptedPacket)
            {
                if (PeersRx.TryGetValue(ep, out peer))
                {
                    UdpEncryptedPacket dataPacket = (UdpEncryptedPacket)packet;
                    byte[] data = Encryption.Decrypt(dataPacket.Data, peer.AesKey);
                    Driver.WriteData(data);
                }
            }
            else if (packet is UdpKeyNegotiationPacket)
            {
                NegotiateKey(peer, packet as UdpKeyNegotiationPacket);
            }
            else if (packet is UdpNewPeerPacket)
            {
                UdpNewPeerPacket newPeerPacket = packet as UdpNewPeerPacket;
                if (newPeerPacket.RecipiantIsNew)
                {
                    CurrentSubnetwork = newPeerPacket.SubnetworkData;
                    IP = newPeerPacket.SubnetworkIP;

                    StartNetworkEngine();
                    StartDriver(CurrentSubnetwork, IP);
                }
                else
                {
                    NegotiateKey(peer, null);
                }
            }
        }

        private void ProcessReceivedData(byte[] data)
        {
            uint sendTo = BitConverter.ToUInt32(data, 16);

            PeerData peer = null;
            if (PeersTx.TryGetValue(sendTo, out peer))
            {
                UdpEncryptedPacket packet =
                    new UdpEncryptedPacket(0, Encryption.Encrypt(data, peer.AesKey));
                data = GetSerializedBytes<UdpEncryptedPacket>(packet);
                Udp.Send(data, data.Length, peer.GetEndpoint());
            }
        }

        private void NegotiateKey(PeerData Peer, UdpKeyNegotiationPacket packet)
        {
            if (packet == null)
            {

            }
            else
            {

            }
        }

        private byte[] GetSerializedBytes<T>(T Obj)
        {
            MemoryStream MS = new MemoryStream();
            Serializer.Serialize<T>(MS, Obj);
            return MS.ToArray();
        }

        private T GetDeserializedObject<T>(byte[] Data)
        {
            MemoryStream MS = new MemoryStream(Data);
            return Serializer.Deserialize<T>(MS);
        }
    }

    public class Statistics
    {
        public double DLSpeed;
        public double ULSpeed;
        public ulong DLPackets;
        public ulong DLBytes;
        public ulong ULPackets;
        public ulong ULBytes;
        public int Peers;
    }

    public class WrongConnectionStringException : Exception
    {
        public WrongConnectionStringException(string message)
            : base(message)
        {

        }
    }
}
