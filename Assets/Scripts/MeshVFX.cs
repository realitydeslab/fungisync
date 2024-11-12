using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
#if UNITY_IOS
using HoloKit;
#endif
using UnityEngine.InputSystem.XR;
using UnityEngine.VFX;

public class MeshVFX : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] ARMeshManager meshManager;
#if UNITY_IOS
    [SerializeField] TrackedPoseDriver trackedPoseDriver;
#elif UNITY_VISIONOS
    [SerializeField] UnityEngine.SpatialTracking.TrackedPoseDriver trackedPoseDriver;
#endif

    [SerializeField] VisualEffect vfx;

    [SerializeField] EnvironmentProbe environmentProbe;


    void LateUpdate()
    {
        if (GameManager.Instance.GameMode == GameMode.Undefined)
            return;


        MeshToBufferConvertor meshToBufferConverter = environmentProbe.MeshToBufferConvertor;

            if (vfx.HasInt("MeshPointCount"))
            {
                
            }
            if (meshToBufferConverter.VertexBuffer != null && vfx.HasGraphicsBuffer("MeshPointCache"))
            {
                vfx.SetGraphicsBuffer("MeshPointCache", meshToBufferConverter.VertexBuffer);
            }
            if (meshToBufferConverter.NormalBuffer != null && vfx.HasGraphicsBuffer("MeshNormalCache"))
            {
                vfx.SetGraphicsBuffer("MeshNormalCache", meshToBufferConverter.NormalBuffer);
            }

        // Push Changes to VFX
        vfx.SetInt("MeshPointCount", meshToBufferConverter.VertexCount);


        // Push Transform to VFX
        // As meshes may not locate at (0,0,0) like they did in iOS.
        // We need to push transform into VFX for converting local position to world position
        IList<MeshFilter> mesh_list = meshManager.meshes;
        if (mesh_list.Count > 0)
        {
            vfx.SetVector3("MeshTransform_position", mesh_list[0].transform.position);
            vfx.SetVector3("MeshTransform_angles", mesh_list[0].transform.rotation.eulerAngles);
            vfx.SetVector3("MeshTransform_scale", mesh_list[0].transform.localScale);
        }
        else
        {
            vfx.SetVector3("MeshTransform_position", Vector3.zero);
            vfx.SetVector3("MeshTransform_angles", Vector3.zero);
            vfx.SetVector3("MeshTransform_scale", Vector3.one);
        }
    }


 }
