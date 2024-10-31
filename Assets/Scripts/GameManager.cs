using UnityEngine;

public class GameManager : MonoBehaviour
{
    public ServerIPSynchronizer serverIPSynchronizer;
    public NetcodeConnectionManager connectionManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void JoinAsClient()
    {
        connectionManager.StartClient(new System.Action<bool, string>((result, msg) => { Debug.Log($"{result}, {msg}"); }));
    }

    public void JoinAsHost()
    {
        connectionManager.StartHost(new System.Action<bool, string>((result, msg) => { Debug.Log($"{result}, {msg}"); }));
    }

    public void JoinAsServer()
    {
        connectionManager.StartServer(new System.Action<bool, string>((result, msg) => { Debug.Log($"{result}, {msg}"); }));
    }

    public void Shutdown()
    {
        connectionManager.ShutDown();
    }
}
