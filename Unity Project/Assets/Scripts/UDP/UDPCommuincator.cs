using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
/// Generic UDP communicator class for sending and receiving data over UDP sockets
/// </summary>
public class UDPCommuincator : MonoBehaviour
{
    public int SenderPort => senderClientPort;
    [SerializeField] private string senderClientTargetIPAddress;
    [SerializeField] private int senderClientPort = 5000;
    protected UdpClient udpSenderClient;

    public int ReceiverPort => receiverPort;
    [SerializeField] protected int receiverPort = 4210;
    [SerializeField] protected string receiverClientErrorName;
    public UdpClient udpRecieverClient;
    protected Coroutine receiverClientCoroutine;

    #region receiverClient
    /// <summary>
    /// Initialises the UDP client at the givne port and then starts the receiver coroutine
    /// </summary>
    /// <param name="port">UDP port to listen on</param>
    protected virtual void StartReceiverClient(int port)
    {
        udpRecieverClient = new UdpClient(port);
        
        StartReceiverCoroutine();
    }

    /// <summary>
    /// Starts or restarts the receiver coroutine.
    /// </summary>
    protected void StartReceiverCoroutine()
    {
        if (receiverClientCoroutine != null)
        {
            StopReceiverCoroutine();
        }
        receiverClientCoroutine = StartCoroutine(ListernForPackets());
    }

    protected void StopReceiverCoroutine()
    {
        StopCoroutine(receiverClientCoroutine);
        receiverClientCoroutine = null;
    }

    /// <summary>
    /// The Receive Coroutine This waits for UDP packet to be recived at <see cref="receiverPort"/>
    /// converts it into a string then calls OnPacketReceived.
    /// The receiving is done asyncronously on another thread to prevent the tie down of the main thread
    /// and as a nice bonus offloads the work from main.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    protected virtual IEnumerator ListernForPackets()
    {
        while (true)
        {
            // recieving is done asyncronously on another thread.
            // The coroutine waits until this thread has finished or faluted.
            var receiver = udpRecieverClient.ReceiveAsync();
            yield return new WaitUntil(() => receiver.IsCompleted);

#if UNITY_EDITOR
            if (receiver.IsFaulted) // this only runs in the editor as android has no easily accessible logs.
            {
                Debug.LogErrorFormat(gameObject, "{0} receiver error", receiverClientErrorName);
                Debug.LogException(receiver.Exception);
            }
#endif
            if (receiver.IsCompletedSuccessfully) // we have data to read
            {
                string receivedData = Encoding.ASCII.GetString(receiver.Result.Buffer);
                OnPacketReceived(receivedData);
            }
        }
    }

    /// <summary>
    /// Method called by <see cref="ListernForPackets"/> when a UDP packet is successfully recieved.
    /// </summary>
    /// <param name="recievedData">Received Data</param>
    protected virtual void OnPacketReceived(string recievedData) { }
    #endregion

    #region SenderClient
    /// <summary>
    /// Initialises the sende client at <see cref="senderClientPort"/> port
    /// Tries to connect to the server at the given IP <see cref="senderClientTargetIPAddress"/>
    /// </summary>
    protected virtual void StartSenderClient()
    {
        udpSenderClient = new UdpClient(senderClientPort) { EnableBroadcast = true };

        try { udpSenderClient.Connect(senderClientTargetIPAddress, senderClientPort); }
        catch (Exception e)
        {
            Debug.LogError("failed to connect to server");
            Debug.LogException(e);
            AndroidLogger.Instance.Log(string.Format("Connection Failure\n{0}", e.ToString()));
            return;
        }
    }
    
    /// <summary>
    /// Used to send data to the server.
    /// </summary>
    /// <param name="data">Data to send to the server</param>
    protected virtual void SendPacket(string data)
    {
        byte[] sendData = Encoding.ASCII.GetBytes(data);
        udpSenderClient.Send(sendData, sendData.Length);
    }
    #endregion
    
    /// <summary>
    /// Make sure to stop all coroutines and close the clients down when the application ends.
    /// </summary>
    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        udpSenderClient?.Close();
        udpRecieverClient?.Close();
    }
}
