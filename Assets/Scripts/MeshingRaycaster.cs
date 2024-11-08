using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

public class MeshingRaycaster : MonoBehaviour
{
    ARMeshManager meshManager;
    TrackedPoseDriver trackedPoseDriver;

    bool didHit = false;
    public bool DidHit { get => didHit; }

    Vector3 hitPosition;
    public Vector3 HitPosition { get => hitPosition; }

    Vector3 hitNormal;
    public Vector3 HitNormal { get => hitNormal; }

    void Awake()
    {
        meshManager = FindFirstObjectByType<ARMeshManager>();
        if (meshManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find meshManager.");
        }

        trackedPoseDriver = FindFirstObjectByType<TrackedPoseDriver>();
        if (trackedPoseDriver == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find TrackedPoseDriver.");
        }
    }

    void LateUpdate()
    {
        if (GameManager.Instance.GameMode == GameMode.Undefined || meshManager == null || trackedPoseDriver == null)
            return;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;


        IList<MeshFilter> mesh_list = meshManager.meshes;
        if (mesh_list == null)
            return;

        Player player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<Player>();
        if (player == null)
            return;


        RaycastHit hit;
        if (Physics.Raycast(player.Body.position, player.Body.forward, out hit))
        {
            didHit = true;
            hitPosition = hit.point;
            hitNormal = hit.normal;
            //Debug.DrawRay(player.Body.position, player.Body.forward * hit.distance, Color.yellow);
            //Debug.Log("Did Hit");
        }
        else
        {
            didHit = false;
            hitPosition = Vector3.zero;
            hitNormal = Vector3.zero ;
            //Debug.DrawRay(player.Body.position, player.Body.forward * 1000, Color.white);
            //Debug.Log("Did not Hit");
        }
    }
}
