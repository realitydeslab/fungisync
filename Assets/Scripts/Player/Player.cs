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
    /// <summary>
    /// Own effect index
    /// </summary>
    public NetworkVariable<int> currentEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>
    /// target effect index which blending towards
    /// </summary>
    public NetworkVariable<int> targetEffectIndex = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    /// <summary>
    /// Lerp value between own effect and target effect
    /// 0 - fully own effect
    /// 1 - fully target effect
    /// </summary>
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

    float faceToFaceAngleThreshold = 100; // angle above which would be consider as face to face

    float maxDistanceThreshold = 1; // Maximum distance thresold within which will blend effect
    float minDistanceThreshold = 0.2f; // Minimum distance threshold within which will be considered as same place

    int handshakeFrameCount = 0;
    int handshakeFrameThreshold = 60;

    


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
        if(role.Value == PlayerRole.Player)// && Time.time - lastChangeEffectTime > effectChangeProtectionTime)
        {
            float min_dis = float.MaxValue;
            Player nearest_player = null;

            CalculatePositionAndOrientation(ref min_dis, ref nearest_player);

            // If someone is near            
            if (nearest_player != null && min_dis < maxDistanceThreshold)
            {
                // Set that player's effect as target effect
                SetTargetEffect(nearest_player.currentEffectIndex.Value);

                // Falloff
                float max_lerp = Utilities.Remap(min_dis, minDistanceThreshold, maxDistanceThreshold, 1, 0, need_clamp:true);
                float lerp_value = Mathf.Min(effectLerp.Value + Time.deltaTime * blendingSpeed, max_lerp);

                if (lerp_value > 1)
                {
                    lerp_value = 1;
                }

                effectLerp.Value = lerp_value;

                // Handshake timer
                if(min_dis < minDistanceThreshold)
                {
                    handshakeFrameCount++;
                    if(handshakeFrameCount > handshakeFrameThreshold)
                    {
                        handshakeFrameCount = handshakeFrameThreshold;
                        // Handshake finished
                    }
                }
                else
                {
                    handshakeFrameCount--;
                    if(handshakeFrameCount < 0)
                    {
                        handshakeFrameCount = 0;
                    }
                }
            }
            else
            {
                float lerp_value = effectLerp.Value - Time.deltaTime * blendingSpeed;                

                if (lerp_value < 0)
                {
                    lerp_value = 0;

                    ClearTargetEffect();
                }

                effectLerp.Value = lerp_value;

                handshakeFrameCount--;
                if (handshakeFrameCount < 0)
                {
                    handshakeFrameCount = 0;
                }
            }

            // UpdateEffect()

            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Player Count", playerManager.PlayerList.Count.ToString());
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Nearest Player", nearest_player == null ? "Null" : nearest_player.OwnerClientId.ToString());
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Distance", nearest_player == null ? "Null" : min_dis.ToString());
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Lerp", effectLerp.Value.ToString());
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("HandshakeFrameCount", handshakeFrameCount.ToString());
        }


        if(role.Value == PlayerRole.Spectator || role.Value == PlayerRole.Host)
        {
            // UpdateEffect()
        }
    }

    void CalculatePositionAndOrientation(ref float min_dis, ref Player nearest_player)
    {
        foreach(var player in playerManager.PlayerList)
        {
            if (player.Key == NetworkManager.Singleton.LocalClientId)
                continue;

            ////////////////////////////////////
            // Logic 1#
            // Both people's hands must be visible
            // Two people must face to face
            if (IsHandVisible(player.Value) == false)
                continue;

            // Both people's hands must be visible
            if (IsFaceToFace(player.Value) == false)
                continue;

            float distance = distance = Vector3.Distance(player.Value.Hand.position, hand.position);

            ////////////////////////////////////
            // Logic 2#
            // Find people who is nearest to interact with
            //float distance = float.MaxValue;
            //if(IsHandVisible(player.Value))
            //{
            //    distance = Vector3.Distance(player.Value.Hand.position, hand.position);
            //}
            //else
            //{
            //    distance = Vector3.Distance(player.Value.Body.position, body.position);
            //}                

            if (distance < min_dis)
            {
                min_dis = distance;
                nearest_player = player.Value;
            }
        }
    }


    /// <summary>
    /// Return true if both people's hands are visible
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool IsHandVisible(Player player)
    {
        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo($"Hand of {player.OwnerClientId}", player.Hand.position.ToString());

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

        Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo($"Angle with {player.OwnerClientId}", angle.ToString());

        return angle > faceToFaceAngleThreshold;
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

    void ClearTargetEffect()
    {
        SetTargetEffect(-1);
    }

    void ChangeEffect(int index)
    {
        Debug.Log($"[{this.GetType()}] Change Effect");
    }

    void SwitchEffect(int index)
    {

    }
}