using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MeshingMaterialManager : MonoBehaviour
{
    List<Material> matList = new List<Material>();

    ARMeshManager arMeshManager;

    void Awake()
    {
        arMeshManager = FindFirstObjectByType<ARMeshManager>();
        if (arMeshManager == null)
        {
            Debug.LogError($"[{this.GetType()}] Can't find ARMeshManager");
        }
    }

    void OnEnable()
    {
        arMeshManager.meshesChanged += OnMeshChanged;
    }
    void OnDisable()
    {
        arMeshManager.meshesChanged -= OnMeshChanged;
    }

    public void RegisterMeshingMaterial(Material mat)
    {
        foreach(var m in matList)
        {
            if (m.name == mat.name)
                return;
        }
        matList.Add(mat);
        UpdateMeshingMaterials();
    }

    public void UnregisterMeshingMaterial(Material mat)
    {
        int index = 0;
        foreach (var m in matList)
        {
            if (m.name == mat.name)
            {
                matList.RemoveAt(index);                
                UpdateMeshingMaterials();
                return;
            }
            index++;
        }
    }

    public void ClearAllMeshingMaterial()
    {
        matList.Clear();
        UpdateMeshingMaterials();
    }

    void OnMeshChanged(ARMeshesChangedEventArgs args)
    {
        UpdateMeshingMaterials();
    }

    void UpdateMeshingMaterials()
    {

        // If want to enable meshing material, uncomment paragraph below

        //Material[] mat_list = matList.ToArray();
        //foreach(var mesh in arMeshManager.meshes)
        //{
        //    mesh.GetComponent<MeshRenderer>().sharedMaterials = mat_list;
        //}
    }
}
