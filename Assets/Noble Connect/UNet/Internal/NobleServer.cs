using NobleConnect.Ice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.NetworkSystem;
using UnityEngine.Networking.Types;

#pragma warning disable 0618

namespace NobleConnect.UNet
{
    /// <summary>Adds relay and punchthrough support to the Unity NetworkServer</summary>
    /// <remarks>
    /// Use the Listen method to start listening for incoming connections.
    /// </remarks>
    public class NobleServer
    {

        #region Public Properties

        static IPEndPoint LocalHostEndPoint = null;
        /// <summary>This is the address that clients should connect to. It is assigned by the relay server.</summary>
        /// <remarks>
        /// Note that this is not the host's actual IP address, but one assigned to the host by the relay server.
        /// When clients connect to this address, Noble Connect will find the best possible connection and use it.
        /// This means that the client may actually end up connecting to an address on the local network, or an address
        /// on the router, or an address on the relay. But you don't need to worry about any of that, it is all
        /// handled for you internally.
        /// </remarks>
        public static IPEndPoint HostEndPoint {
            get {
                if (baseServer != null)
                {
                    return baseServer.RelayEndPoint;
                }
                else
                {
                    return LocalHostEndPoint;
                }
            }
            set {
                LocalHostEndPoint = value;
            }
        }

        /// <summary>Initial timeout before resending refresh messages. This is doubled for each failed resend.</summary>
        public static float allocationResendTimeout = .1f;

        /// <summary>Max number of times to try and resend refresh messages before giving up and shutting down the relay connection.</summary>
        /// <remarks>
        /// If refresh messages fail for 30 seconds the relay connection will be closed remotely regardless of these settings.
        /// </remarks>
        public static int maxAllocationResends = 8;

        /// <summary>How long a relay will stay alive without being refreshed (in seconds)</summary>
        /// <remarks>
        /// Setting this value higher means relays will stay alive longer even if the host temporarily loses connection or otherwise fails to send the refresh request in time.
        /// This can be helpful to maintain connection on an undependable network or when heavy application load (such as loading large levels synchronously) temporarily prevents requests from being processed.
        /// The drawback is that CCU is used for as long as the relay stays alive, so players that crash or otherwise don't clean up properly can cause lingering CCU usage for up to relayLifetime seconds.
        /// </remarks>
        public static int relayLifetime = 60;

        /// <summary>How often to refresh the relay to keep it alive, in seconds</summary>
        public static int relayRefreshTime = 30;

        #endregion Public Properties

        #region Internal Properties

        static Peer baseServer;

        /// <summary>A method to call if something goes wrong like reaching ccu or bandwidth limit</summary>
        static Action<string> onFatalError = null;
        /// <summary>Keeps track of which end point each NetworkConnection belongs to so that when they disconnect we know which Bridge to destroy</summary>
        static Dictionary<NetworkConnection, IPEndPoint> endPointByConnection = new Dictionary<NetworkConnection, IPEndPoint>();

        static IceConfig nobleConfig = new IceConfig();

        #endregion Internal Properties

        #region Public Interface

        /// <summary>Initialize the server using NobleConnectSettings. The region used is determined by the Relay Server Address in the NobleConnectSettings.</summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        public static void InitializeHosting(int listenPort, GeographicRegion region = GeographicRegion.AUTO, HostTopology topo = null, Action<string, ushort> onPrepared = null, Action<string> onFatalError = null, bool forceRelayOnly = false)
        {
            _Init(listenPort, RegionURL.FromRegion(region), topo, onPrepared, onFatalError, forceRelayOnly);
        }

        /// <summary>
        /// Initialize the client using NobleConnectSettings but connect to specific relay server address.
        /// This method is useful for selecting the region to connect to at run time when starting the client.
        /// </summary>
        /// <remarks>\copydetails NobleClient::NobleClient(HostTopology,Action)</remarks>
        /// <param name="relayServerAddress">The url or ip of the relay server to connect to</param>
        /// <param name="topo">The HostTopology to use for the NetworkClient. Must be the same on host and client.</param>
        /// <param name="onPrepared">A method to call when the host has received their HostEndPoint from the relay server.</param>
        /// <param name="onFatalError">A method to call if something goes horribly wrong.</param>
        public static void InitializeHosting(int listenPort, string relayServerAddress, HostTopology topo = null, Action<string, ushort> onPrepared = null, Action<string> onFatalError = null, bool forceRelayOnly = false)
        {
            _Init(listenPort, relayServerAddress, topo, onPrepared, onFatalError, forceRelayOnly);
        }


        /// <summary>Initialize the NetworkServer and Ice.Controller</summary>
        public static void _Init(int listenPort, string relayServerAddress, HostTopology topo = null, Action<string, ushort> onPrepared = null, Action<string> onFatalError = null, bool forceRelayOnly = false)
        {
            UnityLogger.Init();
            
            var settings = (NobleConnectSettings)Resources.Load("NobleConnectSettings", typeof(NobleConnectSettings)); 
            var platform = Application.platform;

            NobleServer.onFatalError = onFatalError;

            nobleConfig = new IceConfig
            {
                iceServerAddress = relayServerAddress,
                icePort = settings.relayServerPort,
                useSimpleAddressGathering = (platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.Android) && !Application.isEditor,
                onFatalError = OnFatalError,
                forceRelayOnly = forceRelayOnly,
                RelayLifetime = relayLifetime,
                RelayRefreshTime = relayRefreshTime,
                RelayRefreshMaxAttempts = maxAllocationResends
            };

            if (!string.IsNullOrEmpty(settings.gameID))
            {
                if (settings.gameID.Length % 4 != 0) throw new System.ArgumentException("Game ID is wrong. Re-copy it from the Dashboard on the website.");
                string decodedGameID = Encoding.UTF8.GetString(Convert.FromBase64String(settings.gameID));
                string[] parts = decodedGameID.Split('\n');

                if (parts.Length == 3)
                {
                    nobleConfig.origin = parts[0];
                    nobleConfig.username = parts[1];
                    nobleConfig.password = parts[2];
                }
            }

            baseServer = new Peer(nobleConfig);

            if (topo != null)
            {
                NetworkServer.Configure(topo);
            }
            NetworkServer.RegisterHandler(MsgType.Connect, OnServerConnect);
            NetworkServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnect);

            if (baseServer == null)
            {
                Logger.Log("NobleServer.Init() must be called before InitializeHosting() to specify region and error handler.", Logger.Level.Fatal);
            }
            baseServer.InitializeHosting(listenPort, onPrepared);
        }

        /// <summary>If you are using the NetworkServer directly you must call this method every frame.</summary>
        /// <remarks>
        /// The NobleNetworkManager and NobleNetworkLobbyManager handle this for you but you if you are
        /// using the NobleServer directly you must make sure to call this method every frame.
        /// </remarks>
        static public void Update()
        {
            if (baseServer != null) baseServer.Update();
        }

        /// <summary>Start listening for incoming connections</summary>
        /// <param name="port"></param>
        /// <param name="onPrepared">A method to call when the host has received their HostEndPoint from the relay server.</param>
        static public void Listen(int port = 0, GeographicRegion region = GeographicRegion.AUTO, HostTopology topo = null, Action<string, ushort> onPrepared = null, Action<string> onFatalError = null, bool forceRelayOnly = false)
        {
            // Store or generate the unet server port
            if (port == 0)
            {
                // Use a randomly generated endpoint
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Bind(new IPEndPoint(IPAddress.Any, 0));
                    port = (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
                }
            }

            InitializeHosting(port, region, topo, onPrepared, onFatalError, forceRelayOnly);

            // UNET Server Go!
            NetworkServer.Listen(port);
        }

        /// <summary>Register a handler for a particular message type.</summary>
        /// <remarks>
        /// There are several system message types which you can add handlers for. You can also add your own message types.
        /// </remarks>
        /// <param name="msgType">Message type number.</param>
        /// <param name="handler">Function handler which will be invoked for when this message type is received.</param>
        static public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
        {
            NetworkMessageDelegate realHandler = handler;
            if (msgType == MsgType.Connect)
            {
                realHandler = (m) => {
                    OnServerConnect(m);
                    handler(m);
                };
            }
            else if (msgType == MsgType.Disconnect)
            {
                realHandler = (m) => {
                    handler(m);
                    OnServerDisconnect(m);
                };
            }
            NetworkServer.RegisterHandler(msgType, realHandler);
        }

        static public void UnregisterHandler(short msgType)
        {
            NetworkServer.UnregisterHandler(msgType);
        }

        static public void ClearHandlers()
        {
            NetworkServer.ClearHandlers();
        }

        /// <summary>Clean up and free resources. Called automatically when garbage collected.</summary>
        /// <remarks>
        /// You shouldn't need to call this directly. It will be called automatically when an unused
        /// NobleServer is garbage collected or when shutting down the application.
        /// </remarks>
        /// <param name="disposing"></param>
        public static void Dispose()
        {
            if (baseServer != null)
            {
                baseServer.Dispose();
                baseServer = null;
            }
        }

        #endregion Public Interface

        #region Handlers

        /// <summary>Called on the server when a client connects</summary>
        /// <remarks>
        /// We store some stuff here so that things can be cleaned up when the client disconnects.
        /// </remarks>
        /// <param name="message"></param>
        static private void OnServerConnect(NetworkMessage message)
        {
            OnServerConnect(message.conn);
        }

        static public void OnServerConnect(NetworkConnection conn)
        {
            if (baseServer == null) return;

            if (conn.hostId != -1)
            {
                int port;
                NetworkID network;
                NodeID node;
                byte error;
                string address;
                NetworkTransport.GetConnectionInfo(conn.hostId, conn.connectionId, out address, out port, out network, out node, out error);

                var clientEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
                endPointByConnection[conn] = clientEndPoint;

                baseServer.FinalizeConnection(clientEndPoint);
            }
        }

        /// <summary>Called on the server when a client disconnects</summary>
        /// <remarks>
        /// Some memory and ports are freed here.
        /// </remarks>
        /// <param name="message"></param>
        static private void OnServerDisconnect(NetworkMessage message)
        {
            OnServerDisconnect(message.conn);
        }

        static public void OnServerDisconnect(NetworkConnection conn)
        {
            if (endPointByConnection.ContainsKey(conn))
            {
                IPEndPoint endPoint = endPointByConnection[conn];
                if (baseServer != null) baseServer.EndSession(endPoint);
                endPointByConnection.Remove(conn);
            }
            conn.Dispose();
        }

        /// <summary>Called when a fatal error occurs.</summary>
        /// <remarks>
        /// This usually means that the ccu or bandwidth limit has been exceeded. It will also
        /// happen if connection is lost to the relay server for some reason.
        /// </remarks>
        /// <param name="errorString">A string with more info about the error</param>
        static private void OnFatalError(string errorString)
        {
            Logger.Log("Shutting down because of fatal error: " + errorString, Logger.Level.Fatal);
            NetworkServer.Shutdown();
            if (onFatalError != null) onFatalError(errorString);
        }

        #endregion Handlers

        #region Static NetworkServer Wrapper
        static public List<NetworkConnection> localConnections { get { return NetworkServer.localConnections; } }
        static public int listenPort { get { return NetworkServer.listenPort; } }
        static public int serverHostId { get { return NetworkServer.serverHostId; } }
        static public ReadOnlyCollection<NetworkConnection> connections { get { return NetworkServer.connections; } }
        static public Dictionary<short, NetworkMessageDelegate> handlers { get { return NetworkServer.handlers; } }
        static public HostTopology hostTopology { get { return NetworkServer.hostTopology; } }
        public static Dictionary<NetworkInstanceId, NetworkIdentity> objects { get { return NetworkServer.objects; } }
        public static bool dontListen { get { return NetworkServer.dontListen; } set { NetworkServer.dontListen = value; } }
        public static bool useWebSockets { get { return NetworkServer.useWebSockets; } set { NetworkServer.useWebSockets = value; } }
        public static bool active { get { return NetworkServer.active; } }
        public static bool localClientActive { get { return NetworkServer.localClientActive; } }
        public static int numChannels { get { return NetworkServer.hostTopology.DefaultConfig.ChannelCount; } }
        public static float maxDelay { get { return NetworkServer.maxDelay; } set { NetworkServer.maxDelay = value; } }
        static public Type networkConnectionClass { get { return NetworkServer.networkConnectionClass; } }
        static public void SetNetworkConnectionClass<T>() where T : NetworkConnection { NetworkServer.SetNetworkConnectionClass<T>(); }
        static public bool Configure(ConnectionConfig config, int maxConnections) { return NetworkServer.Configure(config, maxConnections); }
        static public bool Configure(HostTopology topology) { return NetworkServer.Configure(topology); }
        public static void Reset() { NetworkServer.Reset(); }
        public static void Shutdown() { NetworkServer.Shutdown(); }
        static public NetworkClient BecomeHost(NetworkClient oldClient, int port, MatchInfo matchInfo, int oldConnectionId, PeerInfoMessage[] peers) { return NetworkServer.BecomeHost(oldClient, port, matchInfo, oldConnectionId, peers); }
        static public bool SendToAll(short msgType, MessageBase msg) { return NetworkServer.SendToAll(msgType, msg); }
        static public bool SendToReady(GameObject contextObj, short msgType, MessageBase msg) { return NetworkServer.SendToReady(contextObj, msgType, msg); }
        static public void SendWriterToReady(GameObject contextObj, NetworkWriter writer, int channelId) { NetworkServer.SendWriterToReady(contextObj, writer, channelId); }
        static public void SendBytesToReady(GameObject contextObj, byte[] buffer, int numBytes, int channelId) { NetworkServer.SendBytesToReady(contextObj, buffer, numBytes, channelId); }
        public static void SendBytesToPlayer(GameObject player, byte[] buffer, int numBytes, int channelId) { NetworkServer.SendBytesToPlayer(player, buffer, numBytes, channelId); }
        static public bool SendUnreliableToAll(short msgType, MessageBase msg) { return NetworkServer.SendUnreliableToAll(msgType, msg); }
        static public bool SendUnreliableToReady(GameObject contextObj, short msgType, MessageBase msg) { return NetworkServer.SendUnreliableToReady(contextObj, msgType, msg); }
        static public bool SendByChannelToAll(short msgType, MessageBase msg, int channelId) { return NetworkServer.SendByChannelToAll(msgType, msg, channelId); }
        static public bool SendByChannelToReady(GameObject contextObj, short msgType, MessageBase msg, int channelId) { return NetworkServer.SendByChannelToReady(contextObj, msgType, msg, channelId); }
        static public void DisconnectAll() { NetworkServer.DisconnectAll(); }
        static public void ClearSpawners() { NetworkServer.ClearSpawners(); }
        static public void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
        {
            NetworkServer.GetStatsOut(out numMsgs, out numBufferedMsgs, out numBytes, out lastBufferedPerSecond);
        }
        static public void GetStatsIn(out int numMsgs, out int numBytes)
        {
            NetworkServer.GetStatsIn(out numMsgs, out numBytes);
        }
        static public void SendToClientOfPlayer(GameObject player, short msgType, MessageBase msg)
        {
            NetworkServer.SendToClientOfPlayer(player, msgType, msg);
        }
        static public void SendToClient(int connectionId, short msgType, MessageBase msg)
        {
            NetworkServer.SendToClient(connectionId, msgType, msg);
        }
        static public bool ReplacePlayerForConnection(NetworkConnection conn, GameObject player, short playerControllerId, NetworkHash128 assetId)
        {
            return NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId, assetId);
        }
        static public bool ReplacePlayerForConnection(NetworkConnection conn, GameObject player, short playerControllerId)
        {
            return NetworkServer.ReplacePlayerForConnection(conn, player, playerControllerId);
        }
        static public bool AddPlayerForConnection(NetworkConnection conn, GameObject player, short playerControllerId, NetworkHash128 assetId)
        {
            return NetworkServer.AddPlayerForConnection(conn, player, playerControllerId, assetId);
        }
        static public bool AddPlayerForConnection(NetworkConnection conn, GameObject player, short playerControllerId)
        {
            return NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
        }
        static public void SetClientReady(NetworkConnection conn) { NetworkServer.SetClientReady(conn); }
        static public void SetAllClientsNotReady() { NetworkServer.SetAllClientsNotReady(); }
        static public void SetClientNotReady(NetworkConnection conn) { NetworkServer.SetClientNotReady(conn); }
        static public void DestroyPlayersForConnection(NetworkConnection conn) { NetworkServer.DestroyPlayersForConnection(conn); }
        static public void ClearLocalObjects() { NetworkServer.ClearLocalObjects(); }
        static public void Spawn(GameObject obj) { NetworkServer.Spawn(obj); }
        static public bool SpawnWithClientAuthority(GameObject obj, GameObject player) { return NetworkServer.SpawnWithClientAuthority(obj, player); }
        static public bool SpawnWithClientAuthority(GameObject obj, NetworkConnection conn) { return NetworkServer.SpawnWithClientAuthority(obj, conn); }
        static public bool SpawnWithClientAuthority(GameObject obj, NetworkHash128 assetId, NetworkConnection conn) { return NetworkServer.SpawnWithClientAuthority(obj, assetId, conn); }
        static public void Spawn(GameObject obj, NetworkHash128 assetId) { NetworkServer.Spawn(obj, assetId); }
        static public void Destroy(GameObject obj) { NetworkServer.Destroy(obj); }
        static public void UnSpawn(GameObject obj) { NetworkServer.UnSpawn(obj); }
        static public GameObject FindLocalObject(NetworkInstanceId netId) { return NetworkServer.FindLocalObject(netId); }
        static public Dictionary<short, NetworkConnection.PacketStat> GetConnectionStats() { return NetworkServer.GetConnectionStats(); }
        static public void ResetConnectionStats() { NetworkServer.ResetConnectionStats(); }
        static public bool AddExternalConnection(NetworkConnection conn) { return NetworkServer.AddExternalConnection(conn); }
        static public void RemoveExternalConnection(int connectionId) { NetworkServer.RemoveExternalConnection(connectionId); }
        static public bool SpawnObjects() { return NetworkServer.SpawnObjects(); }

        #endregion
    }
}
