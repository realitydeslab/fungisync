using UnityEngine;
using Unity.Netcode;
using HoloKit.iOS;
using UnityEngine.XR.ARFoundation;

public class LocalPlayerSynchronizer : MonoBehaviour
{
    HandTrackingManager handTrackingManager;
    ARCameraManager arCameraManager;

    void Awake()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find ARCameraManager.");
        }

        handTrackingManager = FindFirstObjectByType<HandTrackingManager>();
        if (handTrackingManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find HandTrackingManager.");
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null || NetworkManager.Singleton.LocalClient.PlayerObject == null
            || NetworkManager.Singleton.LocalClient.PlayerObject.IsSpawned == false)
            return;

        if (GameManager.Instance == null || GameManager.Instance.GameMode == GameMode.Undefined)
            return;

        if (arCameraManager != null)
        {
            Transform body = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(0);
            body.SetPositionAndRotation(arCameraManager.transform.position, arCameraManager.transform.rotation);
        }

        if (handTrackingManager != null)
        {
            Transform hand = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(0);
            hand.position = handTrackingManager.GetHandJointPosition(0, JointName.MiddleMCP);
        }
    }
}
