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

    public NetworkVariable<PlayerRole> role = new NetworkVariable<PlayerRole>(PlayerRole.Player, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> currentEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> targetEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    float effectLerp = 0;

    HandTrackingManager handTrackingManager;
    ARCameraManager arCameraManager;

    Transform body;
    Transform hand;

    void Awake()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if(arCameraManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find ARCameraManager.");
        }

        handTrackingManager = FindFirstObjectByType<HandTrackingManager>();
        if (handTrackingManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find HandTrackingManager.");
        }

        body = transform.GetChild(0);
        hand = transform.GetChild(1);
    }

    void Update()
    {
        if (IsOwner == false)
            return;

        UpdatePositionAndRotation();
    }

    void UpdatePositionAndRotation()
    {
        if (GameManager.Instance == null || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (IsSpawned == false)
            return;

        
        if(arCameraManager != null)
        {
            body.SetPositionAndRotation(arCameraManager.transform.position, arCameraManager.transform.rotation);
        }

        if(handTrackingManager != null)
        {
            hand.position = handTrackingManager.GetHandJointPosition(0, JointName.MiddleMCP);
        }        
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner == false)
            return;

        if (GameManager.Instance != null)
        {
            // Not sure the execution order between OnNetworkSpawn and OnRoleSpecified.
            // Need to make sure that player can be set role correctly.
            // If OnNetworkSpawn is executed before OnRoleSpecified
            GameManager.Instance.OnRoleSpecified.AddListener(OnRoleSpecified);

            // If OnNetworkSpawn is executed after OnRoleSpecified
            if(GameManager.Instance.GameMode != GameMode.Undefined)
            {
                OnRoleSpecified(GameManager.Instance.GameMode, GameManager.Instance.PlayerRole);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner == false)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnRoleSpecified.RemoveListener(OnRoleSpecified);
        }
    }

    void OnRoleSpecified(GameMode game_mode, PlayerRole player_role)
    {
        role.Value = player_role;

        InitializeEffect();
    }

    void InitializeEffect()
    {
        int effect_index = Random.Range(0, 4);
        currentEffectIndex.Value = effect_index;
        targetEffectIndex.Value = effect_index;
        effectLerp = 0;
    }

    void ChangeEffect(int index)
    {

    }

    public void SwitchEffect(int index)
    {

    }
}