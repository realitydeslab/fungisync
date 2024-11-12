using UnityEngine;
using UnityEngine.Events;
using HoloKit;
using HoloKit.ImageTrackingRelocalization;
using Unity.Netcode;
using System.Collections;
using UnityEngine.XR.ARFoundation;
using HoloKit.iOS;

public enum GameMode
{
    Undefined,
    SinglePlayer,
    MultiplePlayer
}

public enum PlayerRole
{
    Undefined,
    /// <summary>
    /// Individual player in network.
    /// Own its own effect.
    /// Can interactive with others.
    /// </summary>
    Player,

    /// <summary>
    /// Can only see other's effect
    /// </summary>
    Spectator,

    /// <summary>
    /// Working as server in network.
    /// Can see other's effect like Spectator
    /// </summary>
    //Host
}

public class GameManager : MonoBehaviour
{
    [SerializeField] bool isInDevelopment = false;
    public bool IsInDevelopment { get => isInDevelopment; set => isInDevelopment = value; }

    [SerializeField] bool takeHostAsPlayer = false;
    public bool TakeHostAsPlayer { get => takeHostAsPlayer; }
    public bool TakeHostAsSpectator { get => !takeHostAsPlayer; }
    

    [Header("References")]
    [SerializeField] NetcodeConnectionManager connectionManager;
    public NetcodeConnectionManager ConnectionManager { get => connectionManager; }

    //[SerializeField] ServerIPSynchronizer serverIPSynchronizer;    
    [SerializeField] ImageTrackingStablizer relocalizationStablizer;
    [SerializeField] EnvironmentProbe environmentProbe;
    [SerializeField] PlayerManager playerManager;
    [SerializeField] EffectManager effectManager;



    GameMode gameMode;
    public GameMode GameMode { get => gameMode; }

    PlayerRole playerRole;
    public PlayerRole PlayerRole { get => playerRole; }

    [Header("Events")]
    public UnityEvent<GameMode, PlayerRole> OnRoleSpecified;

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    #region Network Connection
    public void StartSinglePlayer(System.Action<bool, string> action)
    {
        //// Start Game as singler player
        //StartGame(GameMode.SinglePlayer, PlayerRole.Player);

        //// Update UI
        //action?.Invoke(true, "");


        // To make logic simpler, when play as single player, we make it a host
        StartHost((result, msg) =>
        {
            if (result == true)
            {
                // Start Game as host
                StartGame(GameMode.SinglePlayer, PlayerRole.Player);
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    public void JoinAsPlayer(System.Action<bool, string> action)
    {
        StartClient((result, msg) =>
        {     
            if (result == true)
            {
                // Start Game as player
                StartGame(GameMode.MultiplePlayer, PlayerRole.Player);
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    public void JoinAsSpectator(System.Action<bool, string> action)
    {
        StartClient((result, msg) =>
        {
            if (result == true)
            {
                // Start Game as spectator
                StartGame(GameMode.MultiplePlayer, PlayerRole.Spectator);
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    public void JoinAsHost(System.Action<bool, string> action)
    {
        StartHost((result, msg) =>
        {
            if (result == true)
            {
                // Start Game as host
                StartGame(GameMode.MultiplePlayer, TakeHostAsPlayer ? PlayerRole.Player : PlayerRole.Spectator); //PlayerRole.Host);

                //// Start broadcasting ip
                //serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);
            }

            // Update UI
            action?.Invoke(result, msg);
        });
    }

    void StartClient(System.Action<bool, string> action)
    {
        // 1. Receive Server IP
        //serverIPSynchronizer.StartReceivingServerIp(((server_ip_result, server_ip_msg) =>
        //{
        //if (server_ip_result)
        //{
        //    // 2. Set Server IP
        //    connectionManager.ServerIP = server_ip_msg;

                // 3. Start Connection
                connectionManager.StartClient(((connection_result, connection_msg) => {

                    action?.Invoke(connection_result, connection_msg);

                }));
        //    }
        //    else
        //    {
        //        string msg = server_ip_msg; // if result == false, server_ip refers to error msg
        //        action?.Invoke(false, msg);
        //    }
        //}));        
    }

    void StartHost(System.Action<bool, string> action)
    {
        connectionManager.StartHost(((result, msg) => {
            action?.Invoke(result, msg);
        }));
    }

    void StartServer(System.Action<bool, string> action)
    {
        connectionManager.StartServer(new System.Action<bool, string>((result, msg) => {

            //if (result)
            //    serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);

            action?.Invoke(result, msg);
        }));
    }

    void Shutdown()
    {        
        connectionManager.ShutDown();

        //if (gameMode == GameMode.MultiplePlayer)
        //{
        //    serverIPSynchronizer.ResetConnection();
        //}        
    }

    void WaitForPlayerPrefabSpawned(System.Action<bool, string> action)
    {
        StartCoroutine(WaitForPlayerPrefabSpawnedCoroutine(3, action));
    }

    IEnumerator WaitForPlayerPrefabSpawnedCoroutine(float time_out, System.Action<bool, string> action)
    {
        float start_time = Time.time;
        bool result = true;
        while(NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
        {
            yield return new WaitForSeconds(0.1f);

            if (Time.time - start_time > time_out)
            {
                result = false;
                break;
            }                
        }

        string msg = result ? "Player prefab spawned." : "Waiting for Player prefab spawned times out.";

        Debug.Log($"[{this.GetType()}] {msg}");
        
        action?.Invoke(result, msg);
    }

    void StartGame(GameMode game_mode, PlayerRole player_role)
    {
        WaitForPlayerPrefabSpawned((result, msg) => {

            environmentProbe.EnableEnvironmentProbe();

            SetRole(game_mode, player_role);

            playerManager.InitializePlayerManager(player_role);
        });        
    }

    public void RestartGame(System.Action action)
    {
        // Shutdown network
        Shutdown();

        // Disable EnvironmentProbe
        environmentProbe.DisableEnvironmentProbe();         

        // Reset Player List
        playerManager.ResetPlayerManager();

        // Reset Effect
        effectManager.StopAllEffect();

        // Reset Role
        ResetRole();

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

        // Rotate body by 90 degrees so the fake people representing unity editor can turn around to face real player holding phones
        //OnFinishRelocalization(Vector3.zero, Quaternion.AngleAxis(90, Vector3.up), action);
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
        HoloKit.ColocatedMultiplayerBoilerplate.TrackedImagePoseTransformer trackedImagePoseTransformer;
        trackedImagePoseTransformer = FindFirstObjectByType<HoloKit.ColocatedMultiplayerBoilerplate.TrackedImagePoseTransformer>();

        if(trackedImagePoseTransformer == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find TrackedImagePoseTransformer.");
        }
        else
        {
            trackedImagePoseTransformer.OnTrackedImageStablized(position, rotation);
        }
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

#region Role
    void SetRole(GameMode game_mode, PlayerRole player_role)
    {
        // register callback
        RegisterCallback(game_mode, player_role);

        // update mode and role
        gameMode = game_mode;
        playerRole = player_role;

        Debug.Log($"[{this.GetType()}] Set GameMode to {gameMode} and PlayerRole to {playerRole}");

        // execute callbacks
        OnRoleSpecified?.Invoke(game_mode, player_role);
    }


    /// <summary>
    /// Need to reset role when game stops
    /// 1. click exit game - client/server
    /// 2. suddenly lost - client
    /// </summary>
    void ResetRole()
    {
        // unregister callbacks
        UnregisterCallback(gameMode, playerRole);

        // update mode and role
        gameMode = GameMode.Undefined;
        playerRole = PlayerRole.Player;

        Debug.Log($"[{this.GetType()}] Reset Role");
    }

    /// <summary>
    /// Should register right after role is specified
    /// </summary>
    void RegisterCallback(GameMode game_mode, PlayerRole player_role)
    {
        
        if (game_mode == GameMode.MultiplePlayer)
        {
            connectionManager.OnClientJoinedEvent.AddListener(OnClientJoinedCallback);
            connectionManager.OnClientLostEvent.AddListener(OnClientLostCallback);

            if (player_role == PlayerRole.Player || player_role == PlayerRole.Spectator)
            {
                connectionManager.OnServerLostEvent.AddListener(OnServerLostCallback);
            }
        }
    }

    /// <summary>
    /// Should unregister when game stops
    /// </summary>
    void UnregisterCallback(GameMode game_mode, PlayerRole player_role)
    {
        if (game_mode == GameMode.MultiplePlayer)
        {
            connectionManager.OnClientJoinedEvent.RemoveListener(OnClientJoinedCallback);
            connectionManager.OnClientLostEvent.RemoveListener(OnClientLostCallback);

            if (player_role == PlayerRole.Player || player_role == PlayerRole.Spectator)
            {
                connectionManager.OnServerLostEvent.RemoveListener(OnServerLostCallback);
            }
        }
    }

    void OnClientJoinedCallback(ulong client_id)
    {

    }

    void OnClientLostCallback(ulong client_id)
    {

    }

    void OnServerLostCallback()
    {
        
    }
    #endregion

    #region Effect Manager
    public void ChangeToPreviousEffect()
    {
        if (gameMode != GameMode.SinglePlayer)
            return;

        playerManager.ChangeToPreviousEffect();
    }

    public void ChangeToNextEffect()
    {
        if (gameMode != GameMode.SinglePlayer)
            return;

        playerManager.ChangeToNextEffect();
    }
    #endregion

    public void SpectatePreviousPlayer()
    {
        if (gameMode != GameMode.MultiplePlayer || playerRole != PlayerRole.Spectator)
            return;

        playerManager.SpectatePreviousPlayer();
    }

    public void SpectateNextPlayer()
    {
        if (gameMode != GameMode.MultiplePlayer || playerRole != PlayerRole.Spectator)
            return;

        playerManager.SpectateNextPlayer();
    }


    #region Query Functions
    public bool IsRolePlayer(Player player)
    {
        return player.role.Value == PlayerRole.Player;// || (player.role.Value == PlayerRole.Host && takeHostAsPlayer);
    }

    public bool IsRoleSpectator(Player player)
    {
        return player.role.Value == PlayerRole.Spectator;// || (player.role.Value == PlayerRole.Host && takeHostAsPlayer == false);
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
                    Debug.Log("Can't find GameManager in the scene, will create a new one.");
                    GameObject go = new GameObject();
                    _Instance = go.AddComponent<GameManager>();
                }
            }
            return _Instance;
        }
    }
}
