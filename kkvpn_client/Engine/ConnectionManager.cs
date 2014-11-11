using kkvpn_client.Communication;
using kkvpn_client.Engine;
using kkvpn_client.Misc;
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
        private const int ConnectionRetries = 10;
        private const int UdpReceiveTimeout = 1000;
        private const int NoHeartbeatTimeout = 180;
        private const int TimeBetweenHeartbeats = 30*1000;
        private const string URI = "kkdrv";

        private AppSettings Settings;

        private Subnetwork CurrentSubnetwork;
        private uint IP;
        private UdpClient Udp;
        private bool _Connected;

        private IEncryptionEngine Encryption;
        private DriverConnector Driver;
        private NetworkInterfaceConfiguation InterfaceConfig;
        private KeyExchangeEngine KeyExchange;
        private System.Timers.Timer HeartbeatTimer;

        private Dictionary<UInt32, PeerData> PeersTx;
        private Dictionary<IPEndPoint, PeerData> PeersRx;

        private Statistics Stats;
        private bool _PortForwarded;
        private bool AwaitingExternalConnection;
        private bool DriverStarted;

        private IPEndPoint LocalEndpoint;
        private PeerData Me;

        private ManualResetEvent ReceivedConfirmation;
        private PeerData NewPeer;

        private CancellationTokenSource CancelToken;

        public event EventHandler OnConnected;
        public event EventHandler OnAddedPeer;
        public event EventHandler OnPeerListChanged;
        public event EventHandler OnDisconnected;

        public bool Connected
        {
            get { return _Connected; }
        }

        public bool PortForwarded
        {
            get { return _PortForwarded; }
        }

        public byte[] PublicKey
        {
            get { return KeyExchange.GetPublicKey(); }
        }
    
        public ConnectionManager()
        {
            Stats = new Statistics();

            //cancelTokenSource = new CancellationTokenSource();

            AwaitingExternalConnection = false;
            DriverStarted = false;
            _Connected = false;

            UriRegistrar.RegisterUri(URI, Environment.GetCommandLineArgs()[0]);
        }

        public void InitializeManager()
        {
            Settings = ((App)Application.Current).Settings;

            Driver = new DriverConnector();
            Driver.InitializeDevice();

            InterfaceConfig = new NetworkInterfaceConfiguation();

            Encryption = new PlainTextEngine();
            //Encryption = new AesEngine();
            Encryption.Initialize();
            KeyExchange = new KeyExchangeEngine();
            KeyExchange.InitializeKey();

            HeartbeatTimer = new System.Timers.Timer(TimeBetweenHeartbeats);
            HeartbeatTimer.Elapsed += HeartbeatTimer_Elapsed;
            HeartbeatTimer.Start();
        }

        public Task CreateNewNetwork(string NetworkName, string UserName, uint Address, uint CIDR)
        {
            CancelToken = new CancellationTokenSource();
            return Task.Run(() => 
            {
                if (_Connected)
                {
                    throw new InvalidOperationException("Obecnie jest już otwarte jedno połączenie.");
                }

                try
                {    
                    StartNetworkEngine();
                    StartDriver(
                        new Subnetwork() 
                        { 
                            Address = Address, 
                            CIDR = CIDR, 
                            Name = NetworkName 
                        }, 
                        Address + 1,    // pierwszy dostępny adres
                        UserName
                        );
                }
                catch
                {
                    throw;
                }
            }, 
            CancelToken.Token);
        }

        public Task OpenForConnection()
        {
            CancelToken = new CancellationTokenSource();
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
                catch
                {
                    throw;
                }
            }, 
            CancelToken.Token);
        }

        public void CancelCurrentOperation()
        {
            if (CancelToken != null)
            {
                CancelToken.Cancel();
            }
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
            return URI + ":" + peerData.GetBase64EncodedData();
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
            return Stats.UpdateStats();
        }

        public int GetPeerCount()
        {
            return PeersRx.Count();
        }

        public string GetLowestFreeIP()
        {
            if (PeersTx.Count == 0)
            {
                return (new IPAddress((IP + 1).InvertBytes())).ToString();
            }

            uint[] addresses = PeersTx.Keys.ToArray();
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

        public async void AddPeer(string ConnectionString, uint SubnetworkIP)
        {
            if (!_Connected)
            {
                OnAddedPeer(this, new AddedPeerEventArgs(false, new InvalidOperationException("Żadne połączenie nie jest obecnie otwarte.")));
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

            if (ConnectionString.IndexOf(URI) > -1)
            {
                ConnectionString = ConnectionString.Remove(ConnectionString.IndexOf(URI), URI.Length + 1);
            }

            PeerData peer = null;
            try
            {
                Base64PeerData peerData = Base64PeerData.GetBase64PeerData(ConnectionString);
                peer = PeerData.GetDataFromBase64PeerData(peerData, SubnetworkIP);

                await InitiateNewUser(peer, SubnetworkIP);
                OnAddedPeer(this, new AddedPeerEventArgs(true, null));
            }
            catch (Exception ex)
            {
                RemovePeerFromDictionaries(peer, null);
                OnAddedPeer(this, new AddedPeerEventArgs(false, ex));
            }
        }

        public void Disconnect()
        {
            UdpGoodbye goodbye = new UdpGoodbye();
            Broadcast<UdpGoodbye>(goodbye);

            StopDriver();
            _Connected = false;

            OnDisconnected(this, null);
        }

        public void Dispose()
        {
            UriRegistrar.UnregisterUri(URI);

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
                Logger.Instance.LogMsg("Nie powiodło się otwarcie portu przez UPnP!");
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

            try
            {
                _Connected = true;
                RxThread.Start();
            }
            catch
            {
                _Connected = false;
                throw;
            }
        }

        private void StartDriver(Subnetwork subnetwork, uint localIP, string UserName)
        {
            CurrentSubnetwork = subnetwork;
            IP = localIP;

            Me = new PeerData(
                UserName,
                localIP,
                LocalEndpoint.Address.IPToInt(),
                LocalEndpoint.Port,
                KeyExchange.GetPublicKey()
                );
            AddPeerToDictionaries(Me);

            try
            {
                Driver.SetFilter(subnetwork.Address, subnetwork.CIDR.GetMaskFromCIDR(), localIP);
                InterfaceConfig.AddIP(localIP, subnetwork.CIDR.GetMaskFromCIDR());
                Driver.StartReading(ProcessReceivedDriverData);

                DriverStarted = true;
                OnConnected(this, null);
            }
            catch
            {
                _Connected = false;
                throw;
            }
        }

        private void StopDriver()
        {
            CancelCurrentOperation();

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
            Udp.Client.ReceiveTimeout = UdpReceiveTimeout;
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
                            MessageBox.Show(
                                "Błąd połączenia: " + ex.Message,
                                "Błąd",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                                );
                            Disconnect();
                            break;
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
                    peer.Stats.DLBytes += (ulong)data.Length;
                    peer.Stats.DLPackets++;
                }
            }
            else if (packet is UdpConnectingConfirmation)
            {
                AddPeerToDictionaries(NewPeer);
                if (ReceivedConfirmation != null)
                    ReceivedConfirmation.Set();
            }
            else if (packet is UdpNewPeerPacket)
            {
                UdpNewPeerPacket newPeerPacket = packet as UdpNewPeerPacket;
                if (newPeerPacket.RecipiantIsNew && AwaitingExternalConnection)
                {
                    AddPeersToDictionaries(newPeerPacket.Peers);

                    StartDriver(newPeerPacket.SubnetworkData, newPeerPacket.SubnetworkIP, newPeerPacket.Name);
                    OnConnected(this, null);

                    UdpConnectingConfirmation confirmPacket = new UdpConnectingConfirmation();
                    SendPacket<UdpConnectingConfirmation>(confirmPacket, ep);

                    AwaitingExternalConnection = false;
                    Thread.Sleep(100);
                    NegotiateKey(PeersRx[ep], null);
                }
                else
                {
                    if (newPeerPacket.Peers != null)
                    {
                        if (newPeerPacket.Peers.Length != 0)
                        {
                            AddPeerToDictionaries(newPeerPacket.Peers[0]);
                            NegotiateKey(peer, null);
                        }
                    }
                }
            }
            else if (packet is UdpKeyNegotiationPacket)
            {
                if (PeersRx.TryGetValue(ep, out peer))
                {
                    NegotiateKey(peer, packet as UdpKeyNegotiationPacket);
                }
            }
            else if (packet is UdpGoodbye)
            {
                RemovePeerFromDictionaries(PeersRx[ep], "goodbye");
            }
            else if (packet is UdpHeartbeat)
            {
                if (PeersRx.TryGetValue(ep, out peer))
                {
                    peer.Heartbeat();
                }
            }
        }

        private void AddPeerToDictionaries(PeerData peer)
        {
            if (peer != null)
            {
                PeersRx.Add(peer.GetEndpoint(), peer);
                PeersTx.Add(peer.SubnetworkIP, peer);

                OnPeerListChanged(this, new PeerListChangedEventArgs(PeersRx.Values.ToArray()));
                Logger.Instance.LogMsg("Dodano użytkownika: " + peer.ToString());
            }
        }

        private void RemovePeerFromDictionaries(PeerData peer, string reason)
        {
            if (peer != null)
            {
                if (PeersRx.ContainsKey(peer.GetEndpoint()))
                {
                    PeersRx.Remove(peer.GetEndpoint());
                }
                if (PeersTx.ContainsKey(peer.SubnetworkIP))
                {
                    PeersTx.Remove(peer.SubnetworkIP);
                }

                OnPeerListChanged(this, new PeerListChangedEventArgs(PeersRx.Values.ToArray()));
                if (reason != null)
                {
                    Logger.Instance.LogMsg("Usunięto użytkownika (" + reason + "): " + peer.ToString());
                }
                else
                {
                    Logger.Instance.LogMsg("Usunięto użytkownika: " + peer.ToString());
                }
            }
        }

        private void AddPeersToDictionaries(PeerData[] peers)
        {
            if (peers != null)
            {
                foreach (PeerData peer in peers)
                {
                    PeersRx.Add(peer.GetEndpoint(), peer);
                    PeersTx.Add(peer.SubnetworkIP, peer);
                }
            }
        }

        private void ProcessReceivedDriverData(byte[] data)
        {
            //if (data.Length < 20)
            //{
            //    return;
            //}
            uint sendTo = BitConverter.ToUInt32(data, 16).InvertBytes();

            PeerData peer = null;
            if (PeersTx.TryGetValue(sendTo, out peer))
            {
                EncryptedData encryptedData = Encryption.Encrypt(data, peer.KeyIndex);

                if (encryptedData != null)
                {
                    UdpEncryptedPacket packet =
                        new UdpEncryptedPacket(0, Encryption.Encrypt(data, peer.KeyIndex));
                    SendPacket<UdpEncryptedPacket>(packet, peer.GetEndpoint());

                    Stats.ULBytes += (ulong)data.Length;
                    Stats.ULPackets++;
                    peer.Stats.ULBytes += (ulong)data.Length;
                    peer.Stats.ULPackets++;
                }
            }
        }

        private Task InitiateNewUser(PeerData peer, uint subnetworkIP)
        {
            return Task.Run(() => 
            {
                // pakiet dla nowego użytkownika
                UdpNewPeerPacket packet = new UdpNewPeerPacket(
                    peer.Name,
                    subnetworkIP,
                    true,
                    PeersRx.Values.ToArray(),
                    CurrentSubnetwork
                    );

                byte[] data = GetSerializedBytes<UdpNewPeerPacket>(packet);
               
                ReceivedConfirmation = new ManualResetEvent(false);
                NewPeer = peer;
                int retryCounter = 0;
                while (retryCounter++ < ConnectionRetries && !CancelToken.IsCancellationRequested)
                {
                    Udp.Send(data, data.Length, peer.GetEndpoint());
                    if (ReceivedConfirmation.WaitOne(2000))
                        break;
                }
                NewPeer = null;
                if (retryCounter > ConnectionRetries)
                    throw new NewClientNotReachedException("Czas wywołania minął!", null);
                if (!CancelToken.IsCancellationRequested)
                    return;

                // pakiet dla pozostałych
                packet = new UdpNewPeerPacket(
                    peer.Name,
                    subnetworkIP,
                    false,
                    new PeerData[1] { peer },
                    null
                    );
                data = GetSerializedBytes<UdpNewPeerPacket>(packet);

                if (PeersRx.Count() > 0)
                {
                    foreach (PeerData p in PeersRx.Values)
                    {
                        if (Me != p)
                            Udp.Send(data, data.Length, p.GetEndpoint());
                    }
                }
            });
        }

        private void NegotiateKey(PeerData peer, UdpKeyNegotiationPacket packet)
        {
            if (!peer.KeyExchangeInProgress)
            {
                UdpKeyNegotiationPacket negoPacket = new UdpKeyNegotiationPacket(
                    KeyExchange.GetKeyMaterial(),
                    KeyExchange.GetDSASignature()
                    );
                SendPacket<UdpKeyNegotiationPacket>(negoPacket, peer.GetEndpoint());
            }

            if (packet != null)
            {
                Encryption.DeleteKeyIfInStore(peer.KeyIndex);
                peer.KeyIndex = Encryption.AddKeyToStore(
                    KeyExchange.GetDerivedKey(packet.KeyMaterial, packet.DSASignature, peer.PublicKey)
                    );

                //App.LogMsg(
                //    MiscFunctions.PrintHex(
                //        KeyExchange.GetDerivedKey(packet.KeyMaterial, packet.DSASignature, peer.PublicKey)
                //        )
                //    );
                peer.KeyExchangeInProgress = false;
            }
            else
            {
                peer.KeyExchangeInProgress = true;
            }
        }

        private byte[] GetSerializedBytes<T>(T obj)
        {
            MemoryStream MS = new MemoryStream();
            Serializer.Serialize<T>(MS, obj);
            return MS.ToArray();
        }

        private T GetDeserializedObject<T>(byte[] data)
        {
            MemoryStream MS = new MemoryStream(data);
            return Serializer.Deserialize<T>(MS);
        }

        private void SendPacket<T>(T packet, IPEndPoint ep)
        {
            byte[] data = GetSerializedBytes<T>(packet);
            Udp.Send(data, data.Length, ep);
        }

        private void Broadcast<T>(T packet)
        {
            foreach (PeerData peer in PeersTx.Values)
            {
                if (peer != Me)
                {
                    SendPacket<T>(packet, peer.GetEndpoint());
                }
            }
        }

        private void HeartbeatTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UdpHeartbeat heartbeat = new UdpHeartbeat();
            Broadcast<UdpHeartbeat>(heartbeat);

            foreach (PeerData peer in PeersTx.Values)
            {
                if (peer != Me && peer.Timeout(NoHeartbeatTimeout))
                {
                    RemovePeerFromDictionaries(peer, "timeout");
                }
            }
        }
    }

    class WrongConnectionStringException : Exception
    {
        public WrongConnectionStringException(string message, Exception innerException)
            : base(message, innerException)
        {
           
        }
    }

    class NewClientNotReachedException : Exception
    {
        public NewClientNotReachedException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }

    class AddedPeerEventArgs : EventArgs
    {
        public bool Success;
        public Exception Exc;

        public AddedPeerEventArgs(bool Success, Exception Exc)
            : base()
        {
            this.Success = Success;
            this.Exc = Exc;
        }
    }

    class PeerListChangedEventArgs : EventArgs
    {
        public PeerData[] Peers;

        public PeerListChangedEventArgs(PeerData[] Peers)
            : base()
        {
            this.Peers = Peers;
        }
    }
}
