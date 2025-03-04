using System.Collections;
using HoloKit.iOS;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class EnvironmentProbe : MonoBehaviour
{
    [SerializeField] ARMeshManager arMeshManager;
    [SerializeField] HandTrackingManager handTrackingManager;

    [SerializeField] DepthImageProcessor depthImageProcessor;
    public DepthImageProcessor DepthImageProcessor { get => depthImageProcessor; }
    [SerializeField] MeshToBufferConvertor meshToBufferConvertor;
    public MeshToBufferConvertor MeshToBufferConvertor { get => meshToBufferConvertor; }
    [SerializeField] MeshingRaycaster meshingRaycaster ;
    public MeshingRaycaster MeshingRaycaster { get => meshingRaycaster; }
    [SerializeField] HolokitAudioProcessor audioProcessor;
    public HolokitAudioProcessor AudioProcessor { get => audioProcessor; }


    // XR Environment running in Editor mode has to be enabled at start to correctly recognize meshes.

    void Awake()
    {

        DisableEnvironmentProbe();
#if UNITY_EDITOR
        arMeshManager.enabled = true;

        StartCoroutine(WaitToDisableARMeshManager());
#endif
    }

    IEnumerator WaitToDisableARMeshManager()
    {
        yield return new WaitForSeconds(2);
        arMeshManager.enabled = false;
    }

    public void EnableEnvironmentProbe()
    {
        // Enable AR Functions
        arMeshManager.enabled = true;
#if !UNITY_EDITOR
        handTrackingManager.enabled = true;
#endif

        // Enable Probes
        depthImageProcessor.enabled = true;

        meshToBufferConvertor.enabled = true;

        meshingRaycaster.enabled = true;
    }

    public void DisableEnvironmentProbe()
    {
        arMeshManager.enabled = false;
#if !UNITY_EDITOR
        handTrackingManager.enabled = false;
#endif
        depthImageProcessor.enabled = false;

        meshToBufferConvertor.enabled = false;

        meshingRaycaster.enabled = false;

    }
}
