using UnityEngine;
using UnityEngine.Events;
using HoloKit;
using HoloKit.ImageTrackingRelocalization;
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    bool isInDevelopment = false;
    public bool IsInDevelopment { get => isInDevelopment; set => isInDevelopment = value; }

    public ServerIPSynchronizer serverIPSynchronizer;
    public NetcodeConnectionManager connectionManager;
    public ImageTrackingStablizer relocalizationStablizer;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void RegisterCallback()
    {
        // should register right after role is specified

        //if(NetworkManager.Singleton.IsServer)
        //{
        //    connectionManager.OnClientJoinedEvent += OnClientJoinedCallback;
        //    connectionManager.OnClientLostEvent += OnClientLostCallback;            
        //}

        //if(NetworkManager.Singleton.IsClient)
        //{
        //    connectionManager.OnServerLostEvent += OnServerLostCallback;
        //}        
    }

    void UnregisterCallback()
    {
        // should unregister when game stops

        //if (NetworkManager.Singleton.IsServer)
        //{
        //    connectionManager.OnClientJoinedEvent -= OnClientJoinedCallback;
        //    connectionManager.OnClientLostEvent -= OnClientLostCallback;
        //}

        //if (NetworkManager.Singleton.IsClient)
        //{
        //    connectionManager.OnServerLostEvent -= OnServerLostCallback;
        //}
    }

    #region Network Connection
    public void StartSinglePlayer(System.Action<bool, string> action)
    {
        // Set role to singler player
        //SetRole();

        // register callback

        // Update UI
        action?.Invoke(true, "");
    }

    public void JoinAsPlayer(System.Action<bool, string> action)
    {
        StartClient((result, msg) =>
        {
            // Update UI
            action?.Invoke(result, msg);

            if (result == true)
            {                
                // Set role to player
            }
        });
    }

    public void JoinAsSpectator(System.Action<bool, string> action)
    {
        StartClient((result, msg) =>
        {
            if (result == true)
            {
                // Set role to spectator
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    public void JoinAsServer(System.Action<bool, string> action)
    {
        StartHost((result, msg) =>
        {
            if (result == true)
            {
                // Set role to host
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    void StartClient(System.Action<bool, string> action)
    {
        serverIPSynchronizer.StartReceivingServerIp(((server_ip_result, server_ip_msg) => {
            if(server_ip_result)
            {
                connectionManager.ServerIP = server_ip_msg;

                connectionManager.StartClient(((result, msg) => {
                    Debug.Log($"{result}, {msg}");

                    action?.Invoke(result, msg);
                }));
            }
            else
            {
                string msg = server_ip_msg; // if result == false, server_ip refers to error msg
                action?.Invoke(false, msg);
            }            
        }));        
    }

    void StartHost(System.Action<bool, string> action)
    {
        connectionManager.StartHost(((result, msg) => {
            Debug.Log($"{result}, {msg}");

            if (result)
                serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);

            action?.Invoke(result, msg);
        }));
    }

    void StartServer(System.Action<bool, string> action)
    {
        connectionManager.StartServer(new System.Action<bool, string>((result, msg) => {
            Debug.Log($"{result}, {msg}");

            if (result)
                serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);

            action?.Invoke(result, msg);
        }));
    }

    void Shutdown()
    {
        // if role == network
        {
            connectionManager.ShutDown();

            serverIPSynchronizer.ResetConnection();
        }        
    }

    public void RestartGame(System.Action action)
    {
        Shutdown();

        // set role to undefined

        // remove all callbacks

        // Update UI
        action?.Invoke();
        
    }
    #endregion

    #region Relocalization
    public void StartRelocalization(System.Action action)
    {
#if !UNITY_EDITOR
        UnityAction<Vector3, Quaternion> handler = null;
        handler = (Vector3 position, Quaternion rotation) =>
        {
            relocalizationStablizer.OnTrackedImagePoseStablized.RemoveListener(handler);

            OnFinishRelocalization(position, rotation, action);            
        };
        relocalizationStablizer.OnTrackedImagePoseStablized.AddListener(handler);

        relocalizationStablizer.IsRelocalizing = true;
#else
        OnFinishRelocalization(Vector3.zero, Quaternion.identity, action);
#endif
    }

    public void StopRelocalization()
    {
#if !UNITY_EDITOR
        relocalizationStablizer.OnTrackedImagePoseStablized.RemoveAllListeners() ;

        relocalizationStablizer.IsRelocalizing = false;
#endif
    }

    void OnFinishRelocalization(Vector3 position, Quaternion rotation, System.Action action)
    {
#if !UNITY_EDITOR
            //relocalizationStablizer.OnTrackedImagePoseStablized.RemoveAllListeners();
#endif
        action?.Invoke();
    }

    public float GetRelocalizationProgress()
    {
        if (relocalizationStablizer.IsRelocalizing)
            return relocalizationStablizer.Progress;
        else
            return 0;
    }

#endregion

    private static GameManager _Instance;

    public static GameManager Instance
    {
        get
        {
            if (_Instance == null)
            {
                _Instance = GameObject.FindFirstObjectByType<GameManager>();
                if (_Instance == null)
                {
                    Debug.LogError("Can't find GameManager in the scene!");
                }
            }
            return _Instance;
        }
    }
}
