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
using System.Windows;

namespace kkvpn_client
{
    class ConnectionManager : IDisposable
    {
        private const int UdpPort = 57384;

        private AppSettings Settings;

        private Subnetwork CurrentSubnetwork;
        private uint IP;
        private UdpClient Udp;
        private bool _Connected;

        private IEncryptionEngine Encryption;
        private DriverConnector Driver;
        private NetworkInterfaceConfiguation InterfaceConfig;
        private KeyExchangeEngine KeyExchange;

        private Dictionary<UInt32, PeerData> PeersTx;
        private Dictionary<IPEndPoint, PeerData> PeersRx;

        private int LastSpeedCheck;
        private ulong LastSpeedCheckDL;
        private ulong LastSpeedCheckUL;

        private Statistics Stats;
        private bool _PortForwarded;
        private bool AwaitingExternalConnection;
        private bool DriverStarted;

        private IPEndPoint LocalEndpoint;

        private CancellationTokenSource cancelTokenSource;

        public event EventHandler OnExternalConnected;

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

            cancelTokenSource = new CancellationTokenSource();

            AwaitingExternalConnection = false;
            DriverStarted = false;
            _Connected = false;
        }

        public void InitializeManager()
        {
            Settings = ((App)Application.Current).Settings;

            Driver = new DriverConnector();
            Driver.InitializeDevice();

            InterfaceConfig = new NetworkInterfaceConfiguation();

            Encryption = new PlainTextEngine();
            //Encryption = new AesEngine();
            KeyExchange = new KeyExchangeEngine(false);
            KeyExchange.InitializeKey();
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
                        CIDR = CIDR, 
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
            if (LocalEndpoint == null)
            {
                return "";
            }

            Base64PeerData peerData = new Base64PeerData(
                UserName,
                LocalEndpoint,
                KeyExchange.GetPublicKey()
                );
            return peerData.GetBase64EncodedData();
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
                    {"Mask", (new IPAddress(CurrentSubnetwork.CIDR.GetMaskFromCIDR().InvertBytes())).ToString()},
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

        public string GetLowestFreeIP()
        {
            uint[] keys = PeersTx.Keys.ToArray();
            uint[] addresses = new uint[keys.Length + 1];
            Array.Copy(keys, addresses, keys.Length);
            addresses[addresses.Length - 1] = IP;
            Array.Sort(addresses);

            for (int i = 0; i < addresses.Length - 1; ++i)
            {
                if (addresses[i] + 1 != addresses[i + 1])
                {
                    if (addresses[i] + 1 != IP)
                    {
                        return (new IPAddress(addresses[i] + 1)).ToString();
                    }
                }
            }

            return (new IPAddress((addresses[addresses.Length - 1] + 1).InvertBytes())).ToString();
        }

        public void AddPeer(string ConnectionString, uint SubnetworkIP)
        {
            if (!_Connected)
            {
                throw new InvalidOperationException("Żadne połączenie nie jest obecnie otwarte.");
            }

            if (IP == SubnetworkIP)
            {
                MessageBox.Show(
                    "Wybrany adres IP jest już przypisany do innego użytkownika. Proszę wybrać inny.",
                    "Błąd",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
                return;
            }
            
            foreach (uint ip in PeersTx.Keys)
            {
                if (ip == SubnetworkIP)
                {
                    MessageBox.Show(
                        "Wybrany adres IP jest już przypisany do innego użytkownika. Proszę wybrać inny.",
                        "Błąd",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                    return;
                }
            }

            try
            {
                Base64PeerData peerData = Base64PeerData.GetBase64PeerData(ConnectionString);
                PeerData peer = PeerData.GetDataFromBase64PeerData(peerData, SubnetworkIP);

                InitiateNewUser(peer, SubnetworkIP);
                AddPeerToDictionaries(peer);

                NegotiateKey(peer, null);
            }
            catch (Exception ex)
            {
                throw new WrongConnectionStringException("Błędny connection string!", ex);
            }
        }

        public void Disconnect()
        {
            StopDriver();

            _Connected = false;
        }

        public void Dispose()
        {
            StopDriver();
            Driver.CloseDevice();
        }

        private void StartNetworkEngine()
        {
            Udp = new UdpClient(new IPEndPoint(IPAddress.Any, UdpPort));

            UPnPPortMapper upnp = ((App)Application.Current).UPnP;
            try
            {
                _PortForwarded = upnp.MapPort(UdpPort, UdpPort);
            }
            catch
            {
                _PortForwarded = false;
            }

            if (_PortForwarded)
            {
                LocalEndpoint = new IPEndPoint(upnp.GetExternalIP(), UdpPort);
            }
            else
            {
                IPAddress ip = null;
                IPAddress[] list = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

                foreach (IPAddress i in list)
                {
                    if (i.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ip = i;
                        break;
                    }
                }

                LocalEndpoint = new IPEndPoint(
                    ip?? new IPAddress(0),
                    UdpPort
                    );
            }

            PeersTx = new Dictionary<uint, PeerData>();
            PeersRx = new Dictionary<IPEndPoint, PeerData>();

            Thread RxThread = new Thread(RxWorkerRoutine);
            RxThread.Priority = ThreadPriority.BelowNormal;
            RxThread.Start();

            _Connected = true;
        }

        private void StartDriver(Subnetwork subnetwork, uint localIP)
        {
            Driver.SetFilter(subnetwork.Address, subnetwork.CIDR.GetMaskFromCIDR(), localIP);
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
                catch (Exception ex)
                {
                    if (ex is SocketException)
                    {
                        int code = ((SocketException)ex).ErrorCode;
                        if (code != 10060 && code != 10004)
                        {
                            throw;
                        }
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
                    byte[] data = Encryption.Decrypt(dataPacket.Data, peer.KeyIndex);
                    Driver.WriteData(data);

                    Stats.DLBytes += (ulong)data.Length;
                    Stats.DLPackets++;
                }
            }
            else if (packet is UdpKeyNegotiationPacket)
            {
                NegotiateKey(peer, packet as UdpKeyNegotiationPacket);
            }
            else if (packet is UdpNewPeerPacket)
            {
                UdpNewPeerPacket newPeerPacket = packet as UdpNewPeerPacket;
                if (newPeerPacket.RecipiantIsNew && AwaitingExternalConnection)
                {
                    CurrentSubnetwork = newPeerPacket.SubnetworkData;
                    IP = newPeerPacket.SubnetworkIP;

                    foreach (PeerData p in newPeerPacket.Peers)
                    {
                        AddPeerToDictionaries(p);
                    }

                    Stats.Peers = PeersRx.Count();
                    StartDriver(CurrentSubnetwork, IP);
                    OnExternalConnected(null, null);

                    AwaitingExternalConnection = false;
                }
                else
                {
                    if (newPeerPacket.Peers != null)
                    {
                        if (newPeerPacket.Peers.Length != 0)
                        {
                            AddPeerToDictionaries(newPeerPacket.Peers[0]);
                            Stats.Peers = PeersRx.Count();

                            NegotiateKey(peer, null);
                        }
                    }
                }
            }
        }

        private void AddPeerToDictionaries(PeerData peer)
        {
            PeersRx.Add(peer.GetEndpoint(), peer);
            PeersTx.Add(peer.SubnetworkIP, peer);
        }

        private void ProcessReceivedData(byte[] data)
        {
            uint sendTo = BitConverter.ToUInt32(data, 16);

            PeerData peer = null;
            if (PeersTx.TryGetValue(sendTo, out peer))
            {
                UdpEncryptedPacket packet =
                    new UdpEncryptedPacket(0, Encryption.Encrypt(data, peer.KeyIndex));
                data = GetSerializedBytes<UdpEncryptedPacket>(packet);
                Udp.Send(data, data.Length, peer.GetEndpoint());

                Stats.ULBytes += (ulong)data.Length;
                Stats.ULPackets++;
            }
        }

        private void InitiateNewUser(PeerData peer, uint SubnetworkIP)
        {
            // pakiet dla nowego użytkownika
            UdpNewPeerPacket packet = new UdpNewPeerPacket(
                peer.Name,
                SubnetworkIP,
                true,
                PeersRx.Values.ToArray(),
                CurrentSubnetwork
                );
            byte[] data = GetSerializedBytes<UdpNewPeerPacket>(packet);
            Udp.Send(data, data.Length, peer.GetEndpoint());

            // pakiet dla pozostałych
            packet = new UdpNewPeerPacket(
                peer.Name,
                SubnetworkIP,
                false,
                new PeerData[1] {peer},
                null
                );
            data = GetSerializedBytes<UdpNewPeerPacket>(packet);

            foreach (PeerData p in PeersRx.Values)
            {
                Udp.Send(data, data.Length, p.GetEndpoint());
            }
        }

        private void NegotiateKey(PeerData peer, UdpKeyNegotiationPacket packet)
        {
            if (packet != null)
            {
                Encryption.DeleteKeyIfInStore(peer.KeyIndex);
                peer.KeyIndex = Encryption.AddKeyToStore(KeyExchange.GetDerivedKey(packet.KeyMaterial, peer.PublicKey));
            }

            UdpKeyNegotiationPacket negoPacket = new UdpKeyNegotiationPacket(
                    KeyExchange.GetKeyMaterial()
                    );

            byte[] data = GetSerializedBytes<UdpKeyNegotiationPacket>(negoPacket);
            Udp.Send(data, data.Length, peer.GetEndpoint());
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

    class Statistics
    {
        public double DLSpeed;
        public double ULSpeed;
        public ulong DLPackets;
        public ulong DLBytes;
        public ulong ULPackets;
        public ulong ULBytes;
        public int Peers;
    }

    class WrongConnectionStringException : Exception
    {
        public WrongConnectionStringException(string message, Exception innerException)
            : base(message, innerException)
        {
           
        }
    }
}
