using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using HoloKit.iOS;
using UnityEngine.XR.ARFoundation;
/*
// https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable/
public struct PlayerHand : INetworkSerializable, System.IEquatable<PlayerHand>
{
    public Vector3 position;
    public Quaternion rotation;
    public HandGesture gesture;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsReader)
        {
            var reader = serializer.GetFastBufferReader();
            reader.ReadValueSafe(out position);
            reader.ReadValueSafe(out rotation);
            reader.ReadValueSafe(out gesture);
        }
        else
        {
            var writer = serializer.GetFastBufferWriter();
            writer.WriteValueSafe(position);
            writer.WriteValueSafe(rotation);
            writer.WriteValueSafe(gesture);
        }
    }

    public bool Equals(PlayerHand other)
    {
        return position == other.position && rotation == other.rotation && gesture == other.gesture;
    }
}
*/

public class Player : NetworkBehaviour
{

    public NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(PlayerRole.Undefined, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>
    /// Own effect index
    /// </summary>
    public NetworkVariable<int> currentEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>
    /// target effect index which blending towards
    /// </summary>
    public NetworkVariable<int> targetEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>
    /// Lerp value between own effect and target effect
    /// 0 - fully own effect
    /// 1 - fully target effect
    /// </summary>
    public NetworkVariable<float> effectLerp = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>
    ///
    /// </summary>
    public NetworkVariable<int> handshakeFrameCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<Vector3> handshakeTargetPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> handshakeTargetClientID = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    PlayerManager playerManager;
    EffectManager effectManager;

    Transform body;
    public Transform Body { get => body; }
    Transform hand;
    public Transform Hand { get => hand; }

    public float lastChangeEffectTime = 0;


    void Awake()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find PlayerManager.");
        }

        effectManager = FindFirstObjectByType<EffectManager>();
        if (effectManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find EffectManager.");
        }

        body = transform.GetChild(0);
        hand = transform.GetChild(1);
    }

    void Update()
    {
        if (IsOwner == false || IsSpawned == false)
            return;

        if (GameManager.Instance.GameMode == GameMode.Undefined)
            return;


        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Current Effect", currentEffectIndex.Value.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Current Effect IsOn", effectManager.IsEffectOn(currentEffectIndex.Value).ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Target Effect", targetEffectIndex.Value.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Target Effect IsOn", effectManager.IsEffectOn(targetEffectIndex.Value).ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Lerp", effectLerp.Value.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("HandshakeFrameCount", handshakeFrameCount.Value.ToString());

        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Role", role.Value.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Spectating Player", playerManager.SpectatingPlayerId.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Spectating Player Current Effect", playerManager.ActivePlayer == null ? "Null" : playerManager.ActivePlayer.currentEffectIndex.Value.ToString());
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Spectating Player Target Effect", playerManager.ActivePlayer == null ? "Null" : playerManager.ActivePlayer.targetEffectIndex.Value.ToString());
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentEffectIndex.OnValueChanged += OnCurrentEffectIndexChanged;
        targetEffectIndex.OnValueChanged += OnTargetEffectIndexChanged;

        if (IsOwner == false)
            return;

        if (GameManager.Instance != null)
        {
            // Not sure the execution order between OnNetworkSpawn and OnRoleSpecified.
            // Need to make sure that player can be set role correctly.
            // If OnNetworkSpawn is executed before OnRoleSpecified
            GameManager.Instance.OnRoleSpecified.AddListener(OnRoleSpecified); 

            // If OnNetworkSpawn is executed after OnRoleSpecified
            if (GameManager.Instance.GameMode != GameMode.Undefined)
            {
                OnRoleSpecified(GameManager.Instance.GameMode, GameManager.Instance.PlayerRole);
            }
        }

        Debug.Log("OnNetworkSpawn");
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        currentEffectIndex.OnValueChanged -= OnCurrentEffectIndexChanged;
        targetEffectIndex.OnValueChanged -= OnTargetEffectIndexChanged;

        if (IsOwner == false)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRoleSpecified.RemoveListener(OnRoleSpecified);
        }

        Debug.Log("OnNetworkDespawn");
    }

    void OnRoleSpecified(GameMode game_mode, PlayerRole player_role)
    {
        role.Value = player_role;

        if(player_role == PlayerRole.Spectator)
        {
            playerManager.SpectateNextPlayer();
        }
    }

    void OnCurrentEffectIndexChanged(int prev_index, int new_index)
    {
        //if (role.Value != PlayerRole.Player && (role.Value == PlayerRole.Spectator && playerManager.LocalPlayer != this))
        //    return;
        if (playerManager.ActivePlayer == null || playerManager.ActivePlayer != this)
            return;

        if (prev_index != -1)
            effectManager.StopEffect(prev_index);

        if (new_index != -1)
            effectManager.StartEffect(new_index);
    }

    void OnTargetEffectIndexChanged(int prev_index, int new_index)
    {
        //if (role.Value != PlayerRole.Player && (role.Value != PlayerRole.Spectator && playerManager.ActivePlayer != this))
        //    return;
        if (playerManager.ActivePlayer == null || playerManager.ActivePlayer != this)
            return;

        if (prev_index != -1 && prev_index != currentEffectIndex.Value)
            effectManager.StopEffect(prev_index);

        if (new_index != -1)
            effectManager.StartEffect(new_index);
    }

}