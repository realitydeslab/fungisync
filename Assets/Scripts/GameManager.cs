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
        serverIPSynchronizer.StartReceivingServerIp(new System.Action<string>(server_ip => {
            connectionManager.ServerIP = server_ip;

            connectionManager.StartClient(new System.Action<bool, string>((result, msg) => { Debug.Log($"{result}, {msg}"); }));
        }));        
    }

    public void JoinAsHost()
    {
        connectionManager.StartHost(new System.Action<bool, string>((result, msg) => {
            Debug.Log($"{result}, {msg}");
            if (result)
                serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);
        }));
    }

    public void JoinAsServer()
    {
        connectionManager.StartServer(new System.Action<bool, string>((result, msg) => {
            Debug.Log($"{result}, {msg}");
            if (result)
                serverIPSynchronizer.StartBroadcastingServerIp(connectionManager.ServerIP);
        }));
    }

    public void Shutdown()
    {
        connectionManager.ShutDown();

        serverIPSynchronizer.ResetConnection();
    }
}
