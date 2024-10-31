using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

public class NetcodeConnectionManager : MonoBehaviour
{
    [SerializeField]
    string serverIP = "192.168.0.135";
    public string ServerIP { get => serverIP; set => serverIP = value; }

    [SerializeField]
    ushort serverPort = 7777;
    public ushort Port { get => serverPort; set => serverPort = value; }

    //public ServerIPSynchronizer serverIPSynchronizer;

    string localIP = "";
    public string LocalIP { get => localIP; }

    public UnityEvent<ulong> OnClientJoinedEvent;
    public UnityEvent<ulong> OnClientLostEvent;

    public UnityEvent OnServerLostEvent;

    Action<bool, string> OnReceiveConnectionResultAction;


    enum ConnectionMode
    {
        Undefined,
        Client,
        Server,
        Host
    }
    ConnectionMode connectionMode = ConnectionMode.Undefined;

    void Awake()
    {


    }

    #region Event Listener
    void RegisterCallback()
    {
        Debug.Log("Add All NetCode Listener");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientStarted += OnClientStarted;
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        //networkManager.OnTransportFailure += OnTransportFailure;
        //networkManager.OnConnectionEvent += OnConnectionEvent;
    }

    void UnregisterCallback()
    {
        Debug.Log("Remove All NetCode Listener");
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientStarted -= OnClientStarted;
        NetworkManager.Singleton.OnClientStopped -= OnClientStopped;
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        NetworkManager.Singleton.OnServerStopped -= OnServerStopped;
        //networkManager.OnTransportFailure -= OnTransportFailure;
        //networkManager.OnConnectionEvent -= OnConnectionEvent;
    }

    void OnClientStarted()
    {
        if(OnReceiveConnectionResultAction != null)
        {
            string msg = "Client successfully connected to server.";
            OnReceiveConnectionResultAction?.Invoke(true, msg);
            OnReceiveConnectionResultAction = null;
        }

        Debug.Log("Listener : OnClientStarted");

        localIP = GetLocalIPAddress();
    }

    void OnClientStopped(bool result)
    {
        Debug.Log("Listener : OnClientStopped " + result);


    }

    void OnServerStarted()
    {
        Debug.Log("Listener : OnServerStarted");
        localIP = GetLocalIPAddress();
    }

    void OnServerStopped(bool result)
    {
        Debug.Log("Listener : OnServerStopped " + result);
    }

    //void OnTransportFailure()
    //{
    //    Debug.Log("OnTransportFailure");
    //}

    //void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    //{
    //    //Debug.Log("OnConnectionEvent | " + data.ClientId + "Remain Count " + manager.ConnectedClientsList.Count + "," + manager.ConnectedClients.Count + ", " + manager.ConnectedClientsIds.Count);
    //}

    void OnClientConnectedCallback(ulong client_id)
    {
        Debug.Log(string.Format("OnClientConnectedCallback | IsServer:{0}, IsClient:{1}, IsHost:{2}, ClientID:{3}", NetworkManager.Singleton.IsServer, NetworkManager.Singleton.IsClient, NetworkManager.Singleton.IsHost, client_id));

        // If a new client joined
        if (NetworkManager.Singleton.IsServer)
        {
            OnClientJoinedEvent?.Invoke(client_id);
        }

        if (NetworkManager.Singleton.IsClient)
        {
            string msg = "Connected.";
            OnReceiveConnectionResultAction?.Invoke(true, msg);
            OnReceiveConnectionResultAction = null;
        }


        //// If it's in the process of establishing new connection
        //if (OnReceiveConnectionResultAction != null)
        //{
        //    string msg = "";
        //    if (NetworkManager.Singleton.IsHost)
        //    {
        //        msg = "Host established.";
        //    }
        //    else if (NetworkManager.Singleton.IsServer)
        //    {
        //        msg = "Server established.";
        //    }
        //    else if (NetworkManager.Singleton.IsClient)
        //    {
        //        msg = "Client established.";
        //    }

        //    OnReceiveConnectionResultAction?.Invoke(true, msg);
        //    OnReceiveConnectionResultAction = null;
        //}
        //else
        //{
        //    // If a new client joined
        //    if (NetworkManager.Singleton.IsServer)
        //    {
        //        OnClientJoinedEvent?.Invoke(client_id);
        //    }
        //}
    }

    void OnClientDisconnectCallback(ulong client_id)
    {
        Debug.Log(string.Format("OnClientDisconnectCallback | IsServer:{0}, IsClient:{1}, ClientID:{2}", NetworkManager.Singleton.IsServer, NetworkManager.Singleton.IsClient, client_id));

        // If it's in the process of establishing new connection
        if (OnReceiveConnectionResultAction != null)
        {
            string msg = "";
            if (NetworkManager.Singleton.IsHost)
            {
                msg = "Host establishment failed.";
            }
            else if (NetworkManager.Singleton.IsServer)
            {
                msg = "Server establishment failed.";
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                msg = "Couldn't connect to server.";
            }

            OnReceiveConnectionResultAction?.Invoke(false, msg);
            OnReceiveConnectionResultAction = null;
        }
        else
        {
            // When client left
            if (NetworkManager.Singleton.IsServer)
            {
                OnClientLostEvent?.Invoke(client_id);
            }

            // When client suddenly lost Server
            if (NetworkManager.Singleton.IsClient)
            {
                OnServerLostEvent?.Invoke();
            }
        }

    }
        #endregion

    public bool IsIPAddressValide(string ip)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
    }
    //public void SetServerIP(string ip)
    //{
    //    serverIP = ip;
    //}

    //void StartConnection(ConnectionMode mode, Action<bool, string> callback)
    //{
    //    if (mode == ConnectionMode.Server || mode == ConnectionMode.Host)
    //    {
    //        serverIP = "127.0.0.1";
    //    }

    //    if (IsIPAddressValide(serverIP) == false)
    //    {
    //        string msg = "Server ip is invalid.";

    //        Debug.Log($"[{this.GetType()}] | {msg}");

    //        callback?.Invoke(false, msg);

    //        return;
    //    }


    //    SetConnectionData();

    //    RegisterCallback();

    //    Debug.Log($"[{this.GetType()}] | Starting {mode}.");

    //    bool result = false;

    //    switch(mode)
    //    {
    //        case ConnectionMode.Client:
    //            NetworkManager.Singleton.StartClient();
    //            break;
    //        case ConnectionMode.Client:
    //            NetworkManager.Singleton.StartClient();
    //            break;
    //        case ConnectionMode.Client:
    //            NetworkManager.Singleton.StartClient();
    //            break;
    //    }
            



    //}

    //void SetConnectionData()
    //{
    //    var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    //    if (unityTransport != null)
    //    {
    //        unityTransport.SetConnectionData(serverIP, serverPort); // default is 7777
    //    }
    //}

    #region Public Functions
    public void StartClient(Action<bool, string> callback)
    {
        if (IsIPAddressValide(serverIP) == false)
        {
            string msg = "Server ip is invalid.";

            Debug.Log($"[{this.GetType()}] | {msg}");

            callback?.Invoke(false, msg);

            return;
        }

        if(NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] | Please wait for NetworkManager to initialize");

            return;
        }

        OnBeforeClientStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] | Starting Client.");

        bool result = NetworkManager.Singleton.StartClient();

        if (result == true)
        {
            OnReceiveConnectionResultAction = callback;
        }
        else
        {
            string msg = "Failed to start client.";

            Debug.Log($"[{this.GetType()}] | {msg}");

            callback?.Invoke(false, msg);
        }
    }

    public void StartServer(Action<bool, string> callback)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] | Please wait for NetworkManager to initialize");

            return;
        }

        OnBeforeHostStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] | Starting Server.");

        // Will send back result instantly
        bool result = NetworkManager.Singleton.StartServer();

        string msg = result ? "Server established." : "Failed to start server.";        

        Debug.Log($"[{this.GetType()}] {msg}");

        callback?.Invoke(result, msg);
    }

    public void StartHost(Action<bool, string> callback)
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] | Please wait for NetworkManager to initialize");

            return;
        }

        OnBeforeHostStarted();

        RegisterCallback();

        Debug.Log($"[{this.GetType()}] | Starting Host.");

        // Will send back result instantly
        bool result = NetworkManager.Singleton.StartHost();

        string msg = result ? "Host established." : "Failed to start host.";

        Debug.Log($"[{this.GetType()}] {msg}");

        callback?.Invoke(result, msg);
    }

    public void ShutDown()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log($"[{this.GetType()}] | Please wait for NetworkManager to initialize");

            return;
        }

        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[{this.GetType()}] | Shuting down.");

            NetworkManager.Singleton.Shutdown();
        }
            
    }
    #endregion

    void OnBeforeHostStarted()
    {
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            unityTransport.SetConnectionData("127.0.0.1", serverPort); 
            unityTransport.ConnectionData.ServerListenAddress = "0.0.0.0";
        }
    }
    void OnBeforeClientStarted()
    {
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (unityTransport != null)
        {
            unityTransport.SetConnectionData(serverIP, serverPort); // default is 7777
        }
    }

    string GetLocalIPAddress()
    {

        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork) // && ip.ToString().Contains("192.168"))
            {
                return ip.ToString();
            }
        }
        //throw new System.Exception("No network adapters with an IPv4 address in the system!");
        return "";
    }

}
