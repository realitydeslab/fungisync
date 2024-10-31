using UnityEngine;
using Unity.Netcode;

public class TestOnSpawned : NetworkBehaviour
{
    void Awake()
    {
        Debug.Log("Awake");
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("Start");
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Update");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("Spawned");

        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        Debug.Log("Despawned");
    }
}
