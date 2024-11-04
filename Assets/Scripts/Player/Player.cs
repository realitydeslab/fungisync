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
    public NetworkVariable<float> effectLerp = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    PlayerManager playerManager;
    EffectManager effectManager;

    Transform body;
    public Transform Body { get => body; }
    Transform hand;
    public Transform Hand { get => hand; }

    float blendingSpeed = 1;
    float blendingDirection = 0;
    float lastChangeEffectTime = 0;

    float effectChangeProtectionTime = 5;

    float maxThreshold = 1; // Maximum distance thresold within which will blend effect
    float minThreshold = 0.2f; // Minimum distance threshold within which will be considered as same place

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

        // If it's in Player Mode and has passed 5 seconds since last effect change
        if(role.Value == PlayerRole.Player
            && Time.time - lastChangeEffectTime > effectChangeProtectionTime)
        {
            float min_dis = float.MaxValue;
            Player nearest_player = null;

            CalculatePositionAndOrientation(ref min_dis, ref nearest_player);

            // If someone is near            
            if (nearest_player != null && min_dis < maxThreshold)
            {
                // Set that player's effect as target effect
                SetTargetEffect(nearest_player.currentEffectIndex.Value);

                float max_lerp = 1 - Mathf.SmoothStep(minThreshold, maxThreshold, min_dis);
                float lerp_value = Mathf.Min(effectLerp.Value + Time.deltaTime * blendingSpeed, max_lerp);

                effectLerp.Value = lerp_value;
                if(effectLerp.Value > 1)
                {

                }
            }
            else
            {

            }

            
        }

    }

    void CalculatePositionAndOrientation(ref float min_dis, ref Player nearest_player)
    {
        foreach(var player in playerManager.PlayerList)
        {
            if (player.Key == NetworkManager.Singleton.LocalClientId)
                continue;

            if (IsHandVisible(player.Value) == false)
                continue;

            if (IsFaceToFace(player.Value) == false)
                continue;

            float distance = Vector3.Distance(player.Value.Hand.position, hand.position);

            if(distance < min_dis)
            {
                min_dis = distance;
                nearest_player = player.Value;
            }
        }
    }

    void UpdateBlendingEffect(float min_dis, Player nearest_player)
    {
        
    }

    /// <summary>
    /// Return true if both people's hands are visible
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool IsHandVisible(Player player)
    { 
        return (player.Hand != null && player.Hand.position != Vector3.zero && hand != null && hand.position != Vector3.zero);
    }

    /// <summary>
    /// Return true if the angle between two people's body is larger than threshold
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool IsFaceToFace(Player player)
    {
        if (player.Body == null || body == null)
            return false;

        float angle = Vector3.Angle(player.body.forward, body.forward);
        return angle > 150;
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
        Debug.Log($"[{this.GetType()}] Initialize Effect");

        int effect_index = Random.Range(0, effectManager.EffectCount);

        SetEffect(effect_index);

        SetTargetEffect(-1);

        effectLerp.Value = 0;

        blendingDirection = 0;
    }

    void SetEffect(int effect_index)
    {
        lastChangeEffectTime = Time.time;

        currentEffectIndex.Value = effect_index;

        effectManager.StartEffect(effect_index);
    }

    void SetTargetEffect(int effect_index)
    {
        targetEffectIndex.Value = effect_index;
    }

    void ChangeEffect(int index)
    {
        Debug.Log($"[{this.GetType()}] Change Effect");
    }

    void SwitchEffect(int index)
    {

    }
}