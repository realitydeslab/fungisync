using UnityEngine;
using Unity.Netcode;
using HoloKit.iOS;
using UnityEngine.XR.ARFoundation;

public class LocalPlayerSynchronizer : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] Vector3 fakeBodyPosition;
    [SerializeField] Vector3 fakeBodyRotation;
    [SerializeField] Vector3 fakeHandPosition;
#endif

    HandTrackingManager handTrackingManager;
    ARCameraManager arCameraManager;
    Camera camera;

    void Awake()
    {
        arCameraManager = FindFirstObjectByType<ARCameraManager>();
        if (arCameraManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find ARCameraManager.");
        }
        else
        {
            camera = arCameraManager.gameObject.GetComponent<Camera>();
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



        if (arCameraManager != null && camera != null)
        {
            Transform body = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(0);

#if !UNITY_EDITOR
            body.SetPositionAndRotation(arCameraManager.transform.position, arCameraManager.transform.rotation);
#else
            arCameraManager.transform.SetPositionAndRotation(fakeBodyPosition, Quaternion.Euler(fakeBodyRotation));
            body.SetPositionAndRotation(fakeBodyPosition, Quaternion.Euler(fakeBodyRotation));
#endif
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Body", body.position.ToString());
        }

        if (handTrackingManager != null && camera != null)
        {
            Transform hand = NetworkManager.Singleton.LocalClient.PlayerObject.transform.GetChild(1);

            Vector3 temp_hand_pos = Vector3.zero;
#if !UNITY_EDITOR
            temp_hand_pos = handTrackingManager.GetHandJointPosition(0, JointName.MiddleMCP);
#else
            temp_hand_pos = fakeHandPosition;
#endif
            Vector3 hand_pos_on_screen = camera.WorldToViewportPoint(temp_hand_pos);
            if(hand_pos_on_screen.x >= 0 && hand_pos_on_screen.x<=1
                && hand_pos_on_screen.y >= 0 && hand_pos_on_screen.y <= 1
                && hand_pos_on_screen.z > 0)
            {
                hand.position = temp_hand_pos;
            }
            else
            {
                hand.position = Vector3.zero;
            }
            Xiaobo.UnityToolkit.Helper.HelperModule.Instance.SetInfo("Hand", hand.position.ToString());
        }

        
        
    }
}
