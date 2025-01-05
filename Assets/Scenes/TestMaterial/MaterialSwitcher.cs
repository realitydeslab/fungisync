using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MaterialSwitcher : MonoBehaviour
{
    [SerializeField]
    Material[] matList;

    [SerializeField]
    ARMeshManager arMeshManager;

    int currentMatIndex = -1;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         if(currentMatIndex == -1 && arMeshManager.meshes != null && arMeshManager.meshes.Count > 0)
        {
            SetMeshingMaterial(0);
        }
    }

    public void ChangeToNextMaterial()
    {
        if (matList == null || matList.Length == 0) return;

        int new_mat_index = (currentMatIndex + 1) % matList.Length;

        SetMeshingMaterial(new_mat_index);
    }

    public void ChangeToPreviousMaterial()
    {
        if (matList == null || matList.Length == 0) return;

        int new_mat_index = (currentMatIndex - 1) >= 0 ? (currentMatIndex - 1) : matList.Length - 1;

        SetMeshingMaterial(new_mat_index);
    }

    void SetMeshingMaterial(int index)
    {
        currentMatIndex = index;

        foreach (var mesh in arMeshManager.meshes)
        {
            mesh.GetComponent<MeshRenderer>().material = matList[currentMatIndex];
        }
    }
}
