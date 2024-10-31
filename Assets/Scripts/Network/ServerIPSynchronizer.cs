using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OscJack;
using UnityEngine.Events;


public class ServerIPSynchronizer : MonoBehaviour
{
    private string serverIp = "";
    public string ServerIP
    {
        get => serverIp;
    }

    float receivingTimeOut = 10;

    OscPropertySenderModified oscSender;
    OscEventReceiver oscReceiver;

    void Awake()
    {
        oscSender = gameObject.GetComponent<OscPropertySenderModified>();
        oscReceiver = gameObject.GetComponent<OscEventReceiver>();
    }

    void Start()
    {
        ResetConnection();
    }

    public void StartReceivingServerIp(System.Action<string> action)
    {
        oscReceiver.enabled = true;

        Debug.Log($"[{this.GetType()}] Start receiving ServerIp.");

        TryReceivingServerIp(action);
    }

    IEnumerator TryReceivingServerIp(System.Action<string> action)
    {
        float start_time = Time.time;
        bool result = false;
        while (Time.time - start_time < receivingTimeOut)
        {
            if (serverIp.Length > 0 && IsIPAddressValide(serverIp))
            {
                result = true;
                break;
            }            

            yield return new WaitForSeconds(1);

            Debug.Log($"[{this.GetType()}] Elapsed time: {Time.time - start_time}");
        }

        oscReceiver.enabled = false;
        if (result)
        {
            // successfully received the server ip
            Debug.Log($"[{this.GetType()}] Received ServerIp: {serverIp}");
            action?.Invoke(serverIp);
        }
        else
        {
            // failed to receive the server ip
            Debug.Log($"[{this.GetType()}] Receiving ServerIp time out.");
            action?.Invoke("");
        }
    }

    public void StartBroadcastingServerIp(string ip)
    {
        serverIp = ip;

        // keep sending server ip
        oscSender.enabled = true;

        Debug.Log($"[{this.GetType()}] Start broadcasting ServerIp.");
    }

    public void StopBroadcastingServerIp()
    {
        oscSender.enabled = false;

        Debug.Log($"[{this.GetType()}] Stop broadcasting ServerIp.");
    }

    public void ResetConnection()
    {
        serverIp = "";
        oscSender.enabled = false;
        oscReceiver.enabled = false;
    }

    public bool IsIPAddressValide(string ip)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(ip, @"^((25[0-5]|(2[0-4]|1\d|[1-9]|)\d)\.?\b){4}$");
    }
}
