using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class HandshakeEffect : MonoBehaviour
{
    [SerializeField] VisualEffect vfx1;
    [SerializeField] VisualEffect vfx2;
    PlayerManager playerManager;

    void Awake()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find PlayerManager.");
        }
    }

    void Update()
    {
        if (GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;

        //Player player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();

        Player player = playerManager.ActivePlayer;
        if (player == null || player.IsSpawned == false)
            return;

        //if (GameManager.Instance.IsRolePlayer(player) == false)
        //    return;


        bool is_handshaking = player.handshakeFrameCount.Value > 0 && player.handshakeTargetPosition.Value != Vector3.zero;

        if(is_handshaking)
        {
            float alpha = player.handshakeFrameCount.Value / playerManager.HandshakeFrameThreshold;
            vfx1.SetBool("IsOn", true);
            vfx1.SetVector3("StartPoint", player.Hand.position);
            vfx1.SetVector3("EndPoint", player.handshakeTargetPosition.Value);
            vfx1.SetFloat("Alpha", alpha);

            vfx2.SetBool("IsOn", true);
            vfx2.SetVector3("EndPoint", player.Hand.position);
            vfx2.SetVector3("StartPoint", player.handshakeTargetPosition.Value);
            vfx2.SetBool("ReverseColor", true);
            vfx2.SetFloat("Alpha", alpha);
        }
        else
        {
            vfx1.SetBool("IsOn", false);
            vfx2.SetBool("IsOn", false);
        }
        
    }
}
