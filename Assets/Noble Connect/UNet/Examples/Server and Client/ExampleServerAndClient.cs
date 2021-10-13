using NobleConnect;
using NobleConnect.UNet;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable 0618

// For those that don't want to use Unity's NetworkManager
public class ExampleServerAndClient : MonoBehaviour
{
    [Tooltip("How much logging you want to see.")]
    public NobleConnect.Logger.Level logLevel = NobleConnect.Logger.Level.Info;
    
    [Tooltip("The geographic region to use when selecting a relay server")]
    public GeographicRegion region = GeographicRegion.AUTO;

    // The client. This a wrapper for the UNet NetworkClient with added punchthrough and relay capabilities
    NobleClient client;

    // The host's ip and port
    string hostIPStr = "";
    string hostPortStr = "";
    
    void Start()
    {
        Application.runInBackground = true;
        NobleConnect.Logger.logLevel = logLevel;
        LogFilter.currentLogLevel = (int)logLevel;

        // Constructing the client early helps connections go faster
        client = new NobleClient(region);
        RegisterClientHandlers();
        RegisterServerHandlers();
    }
    
    void Update()
    {
        // You must call the Update method or nothing will work.
        NobleServer.Update();
        if (client != null) client.Update();
    }

    // Display the GUI controls.
    private void OnGUI()
    { 
        HostGUI();
        ClientGUI();
    }

    // Display the host gui
    void HostGUI()
    {
        if (!NobleServer.active)
        {
            // Host button.
            if (client == null || (client != null && (!client.isConnecting && !client.isConnected)))
            {
                if (GUI.Button(new Rect(10, 70, 100, 30), "Host"))
                {
                    Listen();
                }
            }
        }
        else
        {
            // Output the host address.
            if (NobleServer.HostEndPoint != null)
            {
                GUI.Label(new Rect(10, 10, 100, 22), "Host IP:");
                GUI.TextField(new Rect(110, 10, 100, 22), NobleServer.HostEndPoint.Address.ToString(), "Label");
                GUI.Label(new Rect(10, 37, 100, 22), "Host Port:");
                GUI.TextField(new Rect(110, 37, 100, 22), NobleServer.HostEndPoint.Port.ToString(), "Label");
            }
            
            // Disconnect button.
            if (GUI.Button(new Rect(10, 70, 100, 30), "Disconnect"))
            {
                ServerShutdown();
            }
        }
    }

    // Display the client gui
    void ClientGUI()
    {
        if (client == null || !client.isConnected)
        {
            if (client == null || !client.isConnecting)
            {
                // Text boxes for entering the host address.
                GUI.Label(new Rect(240, 10, 100, 22), "Host IP");
                hostIPStr = GUI.TextField(new Rect(340, 10, 100, 22), hostIPStr);
                GUI.Label(new Rect(240, 37, 100, 22), "Host Port");
                hostPortStr = GUI.TextField(new Rect(340, 37, 100, 22), hostPortStr);

                if (GUI.Button(new Rect(240, 70, 100, 30), "Connect"))
                {
                    // Connect to the host's address. For this example this comes from the GUI text boxes
                    // but in the real world you would get it via matchmaking or steam lobbies or similar.
                    // Personally I recommend our Match Up asset.
                    Connect(hostIPStr, ushort.Parse(hostPortStr));
                    Debug.Log("Connecting...");
                }
            }
            else
            {
                GUI.Label(new Rect(240, 10, 150, 22), "Connecting...");
            }
        }
        else
        {
            GUI.Label(new Rect(240, 10, 150, 22), "Client has connected");
            GUI.Label(new Rect(240, 37, 150, 22), "Connection type: " + client.latestConnectionType);
            // Disconnect button
            if (GUI.Button(new Rect(240, 70, 100, 30), "Disconnect"))
            {
                ClientDisconnect();
            }
        }
    }

    // Start listening for incoming connections
    void Listen()
    {
        // Start listening on a random local port, in the selected region, with default host topology.
        // OnServerPrepared will be called when the host has received their relay address
        NobleServer.Listen(0, region, null, OnServerPrepared);
    }
    
    // Connect to a host
    void Connect(string hostIP, ushort hostPort)
    {
        // Connect to the host
        client.Connect(hostIP, hostPort);
    }

    // Register connect and disconnect handlers
    void RegisterClientHandlers()
    {
        client.RegisterHandler(MsgType.Connect, OnClientConnect);
        client.RegisterHandler(MsgType.Disconnect, OnClientDisconnect);
    }

    // Register connect and disconnect handlers.
    void RegisterServerHandlers()
    {
        NobleServer.RegisterHandler(MsgType.Connect, OnServerConnect);
        NobleServer.RegisterHandler(MsgType.Disconnect, OnServerDisconnect);
    }

    // Client connected to server, called on server
    public void OnServerConnect(NetworkMessage message)
    {
        Debug.Log("Server received client connection");
    }

    // Client disconnected from server, called on server
    public void OnServerDisconnect(NetworkMessage message)
    {
        Debug.Log("Server received client disconnect");
    }

    // Client connected to server, called on client
    void OnClientConnect(NetworkMessage message)
    {
        Debug.Log("Client Connected");
    }

    // Client disconnected from server, called on client
    void OnClientDisconnect(NetworkMessage message)
    {
        Debug.Log("Client Disconnected");
    }
    
    // Stop and disconnect the client
    void ClientDisconnect()
    {
        client.connection.Disconnect();
        // You can re-use the same client to make a new connection but if you're totally done with it:
        // Clean up the NobleClient instance
        //client.Dispose();
        //client = null;
    }

    // Shut down and dispose of the server
    void ServerShutdown()
    {
        NobleServer.Shutdown();
        // Clean up
        NobleServer.Dispose();
    }
    
    // Clean up the NobleClient and NobleServer
    void OnDestroy()
    {
        NobleServer.Dispose();
        if (client != null) client.Dispose();
    }

    // OnServerPrepared is called when the host is listening and has received 
    // their HostEndPoint from the NobleConnect service.
    // Use this HostEndPoint on the client in order to connect to the host.
    // Typically you would use a matchmaking system to pass the HostEndPoint to the client.
    // Look at the Match Up Example for one way to do it. Match Up comes free with any paid plan. 
    public void OnServerPrepared(string hostAddress, ushort hostPort)
    {
        // Get your HostEndPoint here. 
        Debug.Log("Hosting at: " + hostAddress + ":" + hostPort);
    }
}