using NobleConnect.Ice;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0618

namespace NobleConnect.UNet
{
    /// <summary>Adds relay, punchthrough, and port-forwarding support to the Unity NetworkClient</summary>
    /// <remarks>
    /// Use the Connect method to connect to a host.
    /// </remarks>
    public class NobleClient
    {
        #region Public Properties

        /// <summary>The NetworkClient that will connect over the bridge to the relay</summary>
        public NetworkClient unetClient;

        private ConnectionType _latestConnectionType = ConnectionType.NONE;

        /// <summary>You can check this in OnClientConnect(), it will either be Direct, Punchthrough, or Relay.</summary>
        public ConnectionType latestConnectionType {
            get {
                if (baseClient != null) return baseClient.latestConnectionType;
                else return _latestConnectionType;
            }
            set {
                _latestConnectionType = value;
            }
        }


        /// <summary>A convenient way to check if a connection is in progress</summary>
        public bool isConnecting = false;

        public static bool active {
            get {
                return NetworkClient.active;
            }
        }

        #endregion

        #region Internal Properties

        /// <summary>The HostTopology to use for the NetworkClient. Must be the same as on the host.</summary>
        HostTopology topo = null;

        /// <summary>Store force relay so that we can pass it on to the iceController</summary>
        public bool ForceRelayOnly;
        /// <summary>A method to call if something goes wrong like reaching ccu or bandwidth limit</summary>
        Action<string> onFatalError = null;

        /// <summary>We store the end point of the local bridge that we connect to because Mirror makes it hard to get the ip and port that a client has connected to for some reason</summary>
        IPEndPoint hostBridgeEndPoint;

        Peer baseClient;
        IceConfig nobleConfig = new IceConfig();

        #endregion Internal Properties

        #region Public Interface

        /// <summary>Initialize the client using NobleConnectSettings. The region used is determined by the Relay Server Address in the NobleConnectSettings.</summary>
        /// <remarks>
        /// The default address is connect.noblewhale.com, which will automatically select the closest 
        /// server based on geographic region.
        /// 
        /// If you would like to connect to a specific region you can use one of the following urls:
        /// <pre>
        ///     us-east.connect.noblewhale.com - Eastern United States
        ///     us-west.connect.noblewhale.com - Western United States
        ///     eu.connect.noblewhale.com - Europe
        ///     ap.connect.noblewhale.com - Asia-Pacific
        ///     sa.connect.noblewhale.com - South Africa
        /// </pre>
        /// 
        /// Note that region selection will ensure each player connects to the closest relay server, but it does not 
        /// prevent players from connecting across regions. If you want to prevent joining across regions you will 
        /// need to implement that separately (by filtering out unwanted regions during matchmaking for example).
        /// </remarks>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(GeographicRegion region = GeographicRegion.AUTO, HostTopology topo = null, Action<string> onFatalError = null, int relayLifetime = 60, int relayRefreshTime = 30, float allocationResendTimeout = .1f, int maxAllocationResends = 8) : base()
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));

            this.onFatalError = onFatalError;
            nobleConfig = new IceConfig
            {
                iceServerAddress = RegionURL.FromRegion(region), // "us-east2.noblewhale.com"
                icePort = settings.relayServerPort,
                RelayRefreshMaxAttempts = maxAllocationResends,
                RelayRequestTimeout = (int)(allocationResendTimeout * 1000),
                RelayLifetime = relayLifetime,
                RelayRefreshTime = relayRefreshTime
            };

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    nobleConfig.username = parts[1];
                    nobleConfig.password = parts[2];
                    nobleConfig.origin = parts[0];
                }
            }

            Init(topo);
        }

        /// <summary>Create a LAN only client</summary>
        /// <remarks>
        /// Clients created in this way will have no relay or punchthrough capabilities.
        /// </remarks>
        public NobleClient(HostTopology topo = null) : base()
        {
            unetClient = new NetworkClient();
            unetClient.Configure(topo);
        }

        /// <summary>Create a NobleClient using an existing NetworkClient</summary>
        /// <remarks>
        /// This is used for the local client on hosts.
        /// </remarks>
        public NobleClient(NetworkClient unetClient) : base()
        {
            this.unetClient = unetClient;
        }

        /// <summary>
        /// Initialize the client using NobleConnectSettings but connect to specific relay server address.
        /// This method is useful for selecting the region to connect to at run time when starting the client.
        /// </summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="relayServerAddress">The url or ip of the relay server to connect to</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        /// <param name="allocationResendTimeout">Initial timeout before resending refresh messages. This is doubled for each failed resend.</param>
        /// <param name="maxAllocationResends">Max number of times to try and resend refresh messages before giving up and shutting down the relay connection. If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.</param>
        public NobleClient(string relayServerAddress, HostTopology topo = null, Action<string> onFatalError = null, int relayLifetime = 60, int relayRefreshTime = 30, float allocationResendTimeout = .1f, int maxAllocationResends = 8)
        {
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings));
            
            this.onFatalError = onFatalError;
            nobleConfig = new IceConfig
            {
                iceServerAddress = relayServerAddress,
                icePort = settings.relayServerPort,
                RelayRefreshMaxAttempts = maxAllocationResends,
                RelayRequestTimeout = (int)(allocationResendTimeout * 1000),
                RelayLifetime = relayLifetime,
                RelayRefreshTime = relayRefreshTime
            };

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    nobleConfig.username = parts[1];
                    nobleConfig.password = parts[2];
                    nobleConfig.origin = parts[0];
                }
            }

            Init(topo);
        }

        /// <summary>Prepare to connect but don't actually connect yet</summary>
        /// <remarks>
        /// This is used when initializing a client early before connecting. Getting this
        /// out of the way earlier can make the actual connection seem quicker.
        /// </remarks>
        public void PrepareToConnect()
        {
            nobleConfig.forceRelayOnly = ForceRelayOnly;
            baseClient.PrepareToConnect();
        }

        /// <summary>If you are using the NetworkClient directly you must call this method every frame.</summary>
        /// <remarks>
        /// The NobleNetworkManager and NobleNetworkLobbyManager handle this for you but you if you are
        /// using the NobleClient directly you must make sure to call this method every frame.
        /// </remarks>
        public void Update()
        {
            if (baseClient != null) baseClient.Update();
        }

        /// <summary>Connect to the provided host ip and port</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostIP">The IP of the server's HostEndPoint</param>
        /// <param name="hostPort">The port of the server's HostEndPoint</param>
        /// <param name="isLANOnly">Connect to LAN host address instead of using relays / punchthrough</param>
        public void Connect(string hostIP, ushort hostPort, bool isLANOnly = false)
        {
            Connect(new IPEndPoint(IPAddress.Parse(hostIP), hostPort), isLANOnly);
        }

        /// <summary>Connect to the provided HostEndPoint</summary>
        /// <remarks>
        /// Note that the host address used here should be the one provided to the host by 
        /// the relay server, not the actual ip of the host's computer. You can get this 
        /// address on the host from Server.HostEndPoint.
        /// </remarks>
        /// <param name="hostEndPoint">The HostEndPoint of the server to connect to</param>
        /// <param name="isLANOnly">Connect to LAN host address instead of using relays / punchthrough</param>
        public void Connect(IPEndPoint hostEndPoint, bool isLANOnly = false)
        {
            if (isConnecting || isConnected) return;
            isConnecting = true;

            if (isLANOnly)
            {
                unetClient.Connect(hostEndPoint);
            }
            else
            {
                if (baseClient == null)
                {
                    Init(topo);
                }
                baseClient.InitializeClient(hostEndPoint, OnReadyToConnect);
            }
        }
        
        /// <summary>Shut down the client and clean everything up.</summary>
        /// <remarks>
        /// You can call this method if you are totally done with a client and don't plan
        /// on using it to connect again.
        /// </remarks>
        public void Shutdown()
        {
            if (baseClient != null)
            {
                baseClient.CleanUpEverything();
                baseClient.Dispose();
                baseClient = null;
            }

            if (unetClient != null) unetClient.Shutdown();
        }

        /// <summary>Clean up and free resources. Called automatically when garbage collected.</summary>
        /// <remarks>
        /// You shouldn't need to call this directly. It will be called automatically when an unused
        /// NobleClient is garbage collected or when shutting down the application.
        /// </remarks>
        /// <param name="disposing"></param>
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (unetClient != null) unetClient.Shutdown();
                if (baseClient != null) baseClient.Dispose();
            }
            isConnecting = false;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion Public Interface

        #region Internal Methods

        /// <summary>Initialize the NetworkClient and Ice.Controller</summary>
        private void Init(HostTopology topo)
        {
            UnityLogger.Init();

            var platform = Application.platform;
            nobleConfig.useSimpleAddressGathering = (platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.Android) && !Application.isEditor;
            nobleConfig.onOfferFailed = CancelConnection;
            nobleConfig.onFatalError = OnFatalError;
            nobleConfig.forceRelayOnly = ForceRelayOnly;
            
            baseClient = new Peer(nobleConfig);

            if (unetClient == null)
            {
                unetClient = new NetworkClient();
                unetClient.RegisterHandler(MsgType.Connect, OnClientConnect);
                unetClient.RegisterHandler(MsgType.Disconnect, OnClientDisconnect);
            }
            if (topo != null)
            {
                unetClient.Configure(topo);
            }
        }

        /// <summary>Stop trying to connect</summary>
        private void CancelConnection()
        {
            if (unetClient != null) unetClient.Disconnect();
            isConnecting = false;
        }

        #endregion Internal Methods

        #region Handlers

        /// <summary>Called when a fatal error occurs.</summary>
        /// <remarks>
        /// This usually means that the ccu or bandwidth limit has been exceeded. It will also
        /// happen if connection is lost to the relay server for some reason.
        /// </remarks>
        /// <param name="errorString">A string with more info about the error</param>
        private void OnFatalError(string errorString)
        {
            Logger.Log("Shutting down because of a fatal error: " + errorString, Logger.Level.Fatal);
            CancelConnection();
            if (onFatalError != null) onFatalError(errorString);
        }


        /// <summary>Called when Ice has selected a candidate pair to use to connect to the host.</summary>
        /// <remarks>
        /// Handles creating the Bridge between Ice and UNet as well as initializing the UNet connection.
        /// </remarks>
        /// <param name="selectedPair">The CandidatePair selected by Ice</param>
        private void OnReadyToConnect(IPEndPoint bridgeEndPoint, IPEndPoint bridgeEndPointIPv6)
        {
            if (Socket.OSSupportsIPv6 && bridgeEndPointIPv6 != null)
            {
                hostBridgeEndPoint = bridgeEndPointIPv6;
                unetClient.Connect(bridgeEndPointIPv6.Address.ToString(), bridgeEndPointIPv6.Port);
            }
            else
            {
                hostBridgeEndPoint = bridgeEndPoint;
                unetClient.Connect(bridgeEndPoint.Address.ToString(), bridgeEndPoint.Port);
            }
        }

        /// <summary>Called on the client upon succesfully connecting to a host</summary>
        /// <remarks>
        /// We clean some ice stuff up here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientConnect(NetworkMessage message)
        { 
            // This happens when connecting in LAN only mode, which is always direct
            if (baseClient == null) latestConnectionType = ConnectionType.DIRECT;

            isConnecting = false;
            if (baseClient != null)
            {
                baseClient.FinalizeConnection(hostBridgeEndPoint);
            }
        }

        /// <summary>Called on the client upon disconnecting from a host</summary>
        /// <remarks>
        /// Some memory and ports are freed here.
        /// </remarks>
        /// <param name="message"></param>
        private void OnClientDisconnect(NetworkMessage message)
        {
            if (baseClient != null) baseClient.EndSession(hostBridgeEndPoint);
        }

        #endregion Handlers

        #region UNET NetworkClient public interface

        #if !DOXYGEN_SHOULD_SKIP_THIS
        /// The rest of this is just a wrapper for UNET's NetworkClient
        public string serverIp { get { return unetClient.serverIp; } }
        public int serverPort { get { return unetClient.serverPort; } }
        public NetworkConnection connection { get { return unetClient.connection; } }
        public Dictionary<short, NetworkMessageDelegate> handlers { get { return unetClient.handlers; } }
        public int numChannels { get { return unetClient.numChannels; } }
        public HostTopology hostTopology { get { return unetClient.hostTopology; } }
        public bool isConnected { get { return unetClient == null ?  false : unetClient.isConnected; } }
        public Type networkConnectionClass { get { return unetClient.networkConnectionClass; } }
        public void SetNetworkConnectionClass<T>() where T : NetworkConnection
        {
            unetClient.SetNetworkConnectionClass<T>();
        }
        public bool Configure(ConnectionConfig config, int maxConnections)
        {
            return unetClient.Configure(config, maxConnections);
        }
        public bool Configure(HostTopology topology)
        {
            return unetClient.Configure(topology);
        }
        public bool ReconnectToNewHost(string serverIp, int serverPort)
        {
            return unetClient.ReconnectToNewHost(serverIp, serverPort);
        }
        public bool ReconnectToNewHost(EndPoint secureTunnelEndPoint)
        {
            return unetClient.ReconnectToNewHost(secureTunnelEndPoint);
        }
        public void ConnectWithSimulator(string serverIp, int serverPort, int latency, float packetLoss)
        {
            unetClient.ConnectWithSimulator(serverIp, serverPort, latency, packetLoss);
        }
        public bool Send(short msgType, MessageBase msg)
        {
            return unetClient.Send(msgType, msg);
        }
        public bool SendWriter(NetworkWriter writer, int channelId)
        {
            return unetClient.SendWriter(writer, channelId);
        }
        public bool SendBytes(byte[] data, int numBytes, int channelId)
        {
            return unetClient.SendBytes(data, numBytes, channelId);
        }
        public bool SendUnreliable(short msgType, MessageBase msg)
        {
            return unetClient.SendUnreliable(msgType, msg);
        }
        public bool SendByChannel(short msgType, MessageBase msg, int channelId)
        {
            return unetClient.SendByChannel(msgType, msg, channelId);
        }
        public void SetMaxDelay(float seconds)
        {
            unetClient.SetMaxDelay(seconds);
        }
        public void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
        {
            unetClient.GetStatsOut(out numMsgs, out numBufferedMsgs, out numBytes, out lastBufferedPerSecond);
        }
        public void GetStatsIn(out int numMsgs, out int numBytes)
        {
            unetClient.GetStatsIn(out numMsgs, out numBytes);
        }
        public Dictionary<short, NetworkConnection.PacketStat> GetConnectionStats()
        {
            return unetClient.GetConnectionStats();
        }
        public void ResetConnectionStats()
        {
            unetClient.ResetConnectionStats();
        }
        public int GetRTT()
        {
            return unetClient.GetRTT();
        }
        public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
        {
            NetworkMessageDelegate realHandler = handler;
            if (msgType == MsgType.Connect)
            {
                realHandler = (m) => {
                    OnClientConnect(m);
                    handler(m);
                };
            }
            else if (msgType == MsgType.Disconnect)
            {
                realHandler = (m) => {
                    handler(m);
                    OnClientDisconnect(m);
                };
            }
            unetClient.RegisterHandler(msgType, realHandler);
        }
        public void RegisterHandlerSafe(short msgType, NetworkMessageDelegate handler)
        {
            unetClient.RegisterHandlerSafe(msgType, handler);
        }
        public void UnregisterHandler(short msgType)
        {
            unetClient.UnregisterHandler(msgType);
        }
        #endif

        #endregion
    }
}
