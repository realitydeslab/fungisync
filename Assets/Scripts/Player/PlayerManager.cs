using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class PlayerManager : NetworkBehaviour
{
    public Dictionary<ulong, Player> PlayerList { get => playerList; }
    protected Dictionary<ulong, Player> playerList = new();

    List<Player> tempPlayerList = new List<Player>();

    EffectManager effectManager;

    public Player ActivePlayer { get => activePlayer; }
    Player activePlayer;
    public int SpectatingPlayerId { get => spectatingPlayerId; }
    int spectatingPlayerId = -1;
    

    public UnityEvent<ulong> OnPlayerJoined;
    public UnityEvent<ulong> OnPlayerLeft;

    float blendingSpeed = 1;

    float effectChangeProtectionTime = 5;
    float viewAngleThreshold = 60; // Below this angle, it is within the field of view

    float maxDistanceThreshold = 1; // Maximum distance thresold within which will blend effect
    float minDistanceThreshold = 0.2f; // Minimum distance threshold within which will be considered as same place

    public int HandshakeFrameThreshold { get => handshakeFrameThreshold; }
    int handshakeFrameThreshold = 180;

    void Awake()
    {
        effectManager = FindFirstObjectByType<EffectManager>();
        if (effectManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find EffectManager.");
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsConnectedClient == false)
            return;

        if (IsSpawned == false)
            return;

        if (GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if(GameManager.Instance.PlayerRole == PlayerRole.Player && IsServer && activePlayer == null)
        {
            activePlayer = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
        }

        /////////////////////////////////////
        /// Both server and client will execute
        /////////////////////////////////////
        // Update Player List
        UpdatePlayerList();

        // Update Local Player
        // PlayerRole.Player: use local player as LocalPlayer
        // PlayerRole.Spectator: specify one of the player as LocalPlayer
        if (GameManager.Instance.PlayerRole == PlayerRole.Player)
        {
            activePlayer = playerList.ContainsKey(NetworkManager.Singleton.LocalClientId) ? playerList[NetworkManager.Singleton.LocalClientId] : null;
        }           
        else if (GameManager.Instance.PlayerRole == PlayerRole.Spectator)
        {
            activePlayer = spectatingPlayerId != -1 && playerList.ContainsKey((ulong)spectatingPlayerId) ? playerList[(ulong)spectatingPlayerId] : null;
        }
        else
        {
            activePlayer = null;
        }



        /////////////////////////////////////
        /// Only server will execute
        /////////////////////////////////////
        if (IsServer == false)
            return;

        ManageHandshakeForAllPlayers();
    }

    void ManageHandshakeForAllPlayers()
    {
        // Update lerp and handshaking
        foreach (var p1 in playerList.Values)
        {
            if (p1 == null)
                continue;

            if (GameManager.Instance.IsRolePlayer(p1))
            {
                if (IsInProtectionPeriod(p1))
                    continue;

                float min_dis = float.MaxValue;
                Player nearest_player = null;

                // Get nearest 
                GetNearestPlayer(p1, ref min_dis, ref nearest_player);


                // If someone is near            
                if (nearest_player != null && min_dis <= maxDistanceThreshold)
                {
                    // Set target effect with other player's effect index
                    SetPlayerTargetEffect(p1, nearest_player.currentEffectIndex.Value, (int)nearest_player.OwnerClientId);


                    // Falloff
                    float max_lerp = Utilities.Remap(min_dis, minDistanceThreshold, maxDistanceThreshold, 1, 0, need_clamp: true);
                    float lerp_value = Mathf.Min(p1.effectLerp.Value + Time.deltaTime * blendingSpeed, max_lerp);

                    if (lerp_value > 1)
                    {
                        lerp_value = 1;
                    }

                    p1.effectLerp.Value = lerp_value;

                    // Handshake timer
                    if (min_dis < minDistanceThreshold)
                    {


                        // Handshake finished
                        if (p1.handshakeFrameCount.Value + 1 > handshakeFrameThreshold)
                        {
                            p1.handshakeFrameCount.Value = handshakeFrameThreshold;

                            bool handshake_state = CheckHandshakeState(p1, nearest_player);

                            if (handshake_state)
                            {
                                SwitchPlayerEffect(p1, nearest_player);
                            }
                        }
                        else
                        {
                            p1.handshakeFrameCount.Value++;
                        }
                    }

                    // Handshake effect
                    if (p1.handshakeFrameCount.Value > 0 && p1.targetEffectIndex.Value != -1)
                    {
                        p1.handshakeTargetPosition.Value = nearest_player.Hand.position;
                    }
                }

                // If no one is near
                if (nearest_player == null || min_dis > maxDistanceThreshold)
                {
                    float lerp_value = p1.effectLerp.Value - Time.deltaTime * blendingSpeed;

                    if (lerp_value < 0)
                    {
                        lerp_value = 0;

                        ClearTargetEffect(p1);
                    }

                    p1.effectLerp.Value = lerp_value;

                    if (p1.handshakeFrameCount.Value - 1 < 0)
                    {
                        p1.handshakeFrameCount.Value = 0;

                        p1.handshakeTargetPosition.Value = Vector3.zero;
                    }
                    else
                    {
                        p1.handshakeFrameCount.Value--;
                    }
                }
            }
        }
    }

    void InitializePlayerEffect(Player player)
    {
        int effect_index = (int)player.OwnerClientId % effectManager.EffectCount;

        SetPlayerEffect(player, effect_index);
    }    

    void SetPlayerEffect(Player player, int effect_index)
    {
        // If it's already the target, then do nothing
        if (player.currentEffectIndex.Value == effect_index)
            return;

        Debug.Log($"[{this.GetType()}] Set Player(id={player.OwnerClientId}) Effect:{effect_index}");

        // update protection time
        player.lastChangeEffectTime = Time.time;

        // update effect index
        // start/stop effect by onValueChange callback
        player.currentEffectIndex.Value = effect_index;
    }

    void SetPlayerTargetEffect(Player player, int effect_index, int owner_id = -1)
    {
        // If it's already the target, then do nothing
        if (player.targetEffectIndex.Value == effect_index)
            return;

        Debug.Log($"[{this.GetType()}] Set Player(id={player.OwnerClientId}) Target Effect:{effect_index}");

        // clear handshake timer
        player.handshakeFrameCount.Value = 0;

        // update target
        player.targetEffectIndex.Value = effect_index;

        // update opponent client id
        player.handshakeTargetClientID.Value = owner_id;
    }

    void ClearTargetEffect(Player player)
    {
        if (player.targetEffectIndex.Value == -1)
            return;

        SetPlayerTargetEffect(player, -1);
    }

    void SwitchPlayerEffect(Player p1, Player p2)
    {
        Debug.Log($"[{this.GetType()}] Switch Player(id={p1.OwnerClientId},effect={p1.currentEffectIndex.Value}) with Player(id={p2.OwnerClientId},effect={p2.currentEffectIndex.Value})");

        SetTargetEffectAsCurrent(p1);

        SetTargetEffectAsCurrent(p2);
    }

    void SetTargetEffectAsCurrent(Player player)
    {
        SetPlayerEffect(player, player.targetEffectIndex.Value);

        SetPlayerTargetEffect(player, -1);

        player.effectLerp.Value = 0;
    }

    public void ChangeToNextEffect()
    {
        if (IsServer == false)
            return;

        if (playerList.Count <= 0)
            return;

        if (playerList[0] == null || playerList[0].IsSpawned == false)
            return;

        int new_effect_index = (playerList[0].currentEffectIndex.Value + 1) % effectManager.EffectCount;
        foreach(var player in playerList.Values)
        {
            SetPlayerEffect(player, new_effect_index);
        }
        
    }

    public void ChangeToPreviousEffect()
    {
        if (IsServer == false)
            return;

        if (playerList.Count <= 0)
            return;

        if (playerList[0] == null || playerList[0].IsSpawned == false)
            return;

        int new_effect_index = (playerList[0].currentEffectIndex.Value - 1);
        if (new_effect_index < 0)
            new_effect_index = effectManager.EffectCount - 1;
        foreach (var player in playerList.Values)
        {
            SetPlayerEffect(player, new_effect_index);
        }
    }

    bool CheckHandshakeState(Player p1, Player p2)
    {
        return (p1.handshakeFrameCount.Value == handshakeFrameThreshold && p1.targetEffectIndex.Value == p2.currentEffectIndex.Value
            && p2.handshakeFrameCount.Value == handshakeFrameThreshold && p2.targetEffectIndex.Value == p1.currentEffectIndex.Value);
    }

    void GetNearestPlayer(Player p1, ref float min_dis, ref Player nearest_player)
    {
        foreach (var p2 in playerList.Values)
        {
            if (p1.OwnerClientId == p2.OwnerClientId)
                continue;

            if (GameManager.Instance.IsRolePlayer(p2) == false)
                continue;

            if (IsInProtectionPeriod(p2))
                continue;

            ////////////////////////////////////
            // Logic 1#
            // Both people's hands must be visible            
            if (IsHandVisible(p1, p2) == false)
                continue;

            // Two people must face to face
            if (IsFaceToFace(p1, p2) == false)
                continue;

            float distance = distance = Vector3.Distance(p1.Hand.position, p2.Hand.position);

            if (distance < min_dis)
            {
                min_dis = distance;
                nearest_player = p2;
            }
        }
    }

    bool IsInProtectionPeriod(Player player)
    {
        return Time.time - player.lastChangeEffectTime < effectChangeProtectionTime;
    }

    /// <summary>
    /// Return true if both people's hands are visible
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool IsHandVisible(Player p1, Player p2)
    {
        return (p1.Hand != null && p1.Hand.position != Vector3.zero && p2.Hand != null && p2.Hand.position != Vector3.zero);
    }

    /// <summary>
    /// Return true if the angle between two people's body is larger than threshold
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    bool IsFaceToFace(Player p1, Player p2)
    {
        if (p1.Body == null || p2.Body == null)
            return false;

        float angle_other_to_self = GetViewAngle(p1, p2.Body.position);

        float angle_self_to_other = GetViewAngle(p2, p1.Body.position);

        bool result = angle_other_to_self < viewAngleThreshold && angle_self_to_other < viewAngleThreshold;

        return result;
    }

    float GetViewAngle(Player player, Vector3 pos)
    {
        if (player.Body == null)
            return -1;

        float angle = Vector3.Angle(player.Body.forward, pos - player.Body.position);

        return angle;
    }

    void UpdatePlayerList()
    {
        if (GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null)
            return;        

        // Get all player's gameobject
        var gameobject_list = GameObject.FindGameObjectsWithTag("Player");

        // Filter out player that has not finished spawning
        // filter out all spectators
        tempPlayerList.Clear();
        for (int i = 0; i < gameobject_list.Length; i++)
        {
            Player p = gameobject_list[i].GetComponent<Player>();
            if (p.IsSpawned == false || p.role.Value == PlayerRole.Undefined)
                continue;

            if (p.role.Value == PlayerRole.Spectator)
                continue;

            tempPlayerList.Add(p);
        }



        // check if player left
        List<ulong> player_to_be_removed = new();
        foreach (var player in playerList)
        {
            ulong client_id = player.Key;

            bool exist = false;
            for (int i = 0; i < tempPlayerList.Count; i++)
            {
                if (tempPlayerList[i].OwnerClientId == client_id)
                {
                    exist = true;
                    break;
                }
            }

            if (exist == false)
            {
                player_to_be_removed.Add(client_id);
            }
        }

        // remove player if needed
        bool need_to_update_spectating_player = false;
        for (int i = 0; i < player_to_be_removed.Count; i++)
        {
            ulong client_id = player_to_be_removed[i];

            playerList.Remove(client_id);

            OnPlayerLeft?.Invoke(client_id);

            if (GameManager.Instance.PlayerRole == PlayerRole.Spectator && spectatingPlayerId != -1 && (int)client_id == spectatingPlayerId)
                need_to_update_spectating_player = true;

            Debug.Log($"[{ this.GetType()}] Player {client_id} Left. Player Count:{playerList.Count}");
        }

        // if player being speclated has been removed, update spectating player
        if(need_to_update_spectating_player)
        {
            OnLostSpectatingPlayer();
        }        


        // check if new player joined
        for (int i = 0; i < tempPlayerList.Count; i++)
        {
            ulong client_id = tempPlayerList[i].OwnerClientId;
            if (playerList.ContainsKey(client_id) == false)
            {
                playerList.Add(client_id, tempPlayerList[i]);

                OnPlayerJoined?.Invoke(client_id);

                // Only server can specify effect for newer player
                if(IsServer)
                {
                    InitializePlayerEffect(tempPlayerList[i]);
                }
                    

                Debug.Log($"[{ this.GetType()}] Player {client_id} Joined. Player Count:{playerList.Count}");
            }
        }
    }

    public void SetLocalPlayer(Player player)
    {
        activePlayer = player;
    }

    public void ResetPlayerManager()
    {
        activePlayer = null;

        spectatingPlayerId = -1;

        if(IsServer)
        {
            playerList.Clear();
        }
    }

    

    public void SpectateNextPlayer()
    {
        if (GameManager.Instance.PlayerRole != PlayerRole.Spectator)
            return;

        if (playerList.Count == 0)
            return;

        int current_player_index = GetIndexByClientID(spectatingPlayerId);

        int new_player_index = -1;
        if(current_player_index == -1)
        {
            new_player_index = 0;
        }
        else
        {
            new_player_index = (current_player_index + 1) % playerList.Count;
        }

        int client_id = GetClientIDByIndex(new_player_index);

        Debug.Log($"[{this.GetType()}] Spectate Next Player:{client_id}");

        StartSpectatingPlayer(client_id);
    }

    public void SpectatePreviousPlayer()
    {
        if (GameManager.Instance.PlayerRole != PlayerRole.Spectator)
            return;

        if (playerList.Count == 0)
            return;

        int current_player_index = GetIndexByClientID(spectatingPlayerId);

        int new_player_index = -1;
        if (current_player_index == -1)
        {
            new_player_index = playerList.Count - 1;
        }
        else
        {
            new_player_index = current_player_index - 1;
            if(new_player_index < 0)
                new_player_index = playerList.Count - 1;
        }

        int client_id = GetClientIDByIndex(new_player_index);

        Debug.Log($"[{this.GetType()}] Spectate Previous Player:{client_id}");

        StartSpectatingPlayer(client_id);
    }

    void StartSpectatingPlayer(int client_id)
    {
        if (client_id == -1)
            return;

        if (playerList.ContainsKey((ulong)client_id) == false)
            return;

        spectatingPlayerId = client_id;

        Player player = playerList[(ulong)client_id];

        if (player.currentEffectIndex.Value != -1)
            effectManager.StartEffect(player.currentEffectIndex.Value);

        if (player.targetEffectIndex.Value != -1)
            effectManager.StartEffect(player.targetEffectIndex.Value);
    }

    void RemoveSpectatingPlayer()
    {
        effectManager.StopAllEffect();

        spectatingPlayerId = -1;
    }

    void OnLostSpectatingPlayer()
    {
        RemoveSpectatingPlayer();

        SpectateNextPlayer();
    }

    int GetClientIDByIndex(int player_index)
    {
        int client_id = -1;
        int index = 0;
        foreach(var player in playerList)
        {
            if(index == player_index)
            {
                client_id = (int)player.Key;
                break;
            }
            index++;
        }
        return client_id;
    }

    int GetIndexByClientID(int client_id)
    {
        int player_index = -1;
        int index = 0;
        foreach (var player in playerList)
        {
            if (client_id == (int)player.Key)
            {
                player_index = index;
                break;
            }
            index++;
        }
        return player_index;
    }



    //private static PlayerManager _Instance;

    //public static PlayerManager Instance
    //{
    //    get
    //    {
    //        if (_Instance == null)
    //        {
    //            _Instance = GameObject.FindFirstObjectByType<PlayerManager>();
    //            if (_Instance == null)
    //            {
    //                Debug.Log("Can't find PlayerManager in the scene, will create a new one.");
    //                GameObject go = new GameObject();
    //                _Instance = go.AddComponent<PlayerManager>();
    //            }
    //        }
    //        return _Instance;
    //    }
    //}
}
