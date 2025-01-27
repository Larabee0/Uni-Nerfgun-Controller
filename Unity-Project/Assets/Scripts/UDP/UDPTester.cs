using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading.Tasks;
using UnityEngine.Android;

/// <summary>
/// Development script used for testing UDP sockets, send and recieving UDP packets between the ESP8266 and Unity.
/// This script was used for checking if data returned by the arduino was correct for the imu and the gun state - seperately.
/// This was done by sending a packet every <see cref="updateTime"/> then waiting for a returning packet of json.
/// 
/// While developing this I found several quirks of android, the most important being you need seperate UdpClient Instances to send and recieve.
/// Another minor one being hte requirement of "EnableBroadcast = true" during initialisation of the sender Client.
/// The most annoying was the requirement to turn off mobile data on android. This allowed reception of data but prevented sending.
/// </summary>
public class UDPTester : MonoBehaviour
{
    [SerializeField] private int sendPort = 5000;
    [SerializeField] private int ReceivePort = 4210;
    [SerializeField] private string ipAddress = "192.168.4.1";
    [SerializeField] private float updateTime = 5f;

    [SerializeField] private NerfgunState state;
    [SerializeField] private NerfgunCommandRequest command;

    [SerializeField] private IMUQuat imu;
    [SerializeField] private Transform imuEffectee;
    [SerializeField] private Vector3 eulerAngle;

    private UdpClient udpSendClient;
    private UdpClient udpRecieveClient;

    /// <summary>
    /// I thought it would be best to close the ports when the game object is disabled, hence we run OnEnable and OnDisable to achieve it.
    /// </summary>
    private void OnEnable()
    {
        udpSendClient = new UdpClient(sendPort) { EnableBroadcast = true };
        udpRecieveClient = new UdpClient(ReceivePort);

        try
        {
            udpSendClient.Connect(ipAddress, sendPort);
            Debug.Log("connected to server");
        }
        catch (Exception e)
        {
            Debug.LogWarning("failed to connect to server");
            Debug.LogErrorFormat("failed to connect to server! Exception Caught! Message: {0}", e.Message);
            AndroidLogger.Instance.Log(e.Message);
            return;
        }

        // main coroutines for sending and receiving. I started with the mainthread variant before transitioning to the Async.
        StartCoroutine(TrySend());
        // StartCoroutine(TryListenMainThread());
        StartCoroutine(TryListenAsync());

    }

    /// <summary>
    /// Method used to test sending json data to the ESP8266, and then deserializing it.
    /// This sends the <see cref="command"/> struct every <see cref="updateTime"/>.
    /// When the ESP8266 receives this packet it sends back something for the listen coroutine to interpret.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator TrySend()
    {
        AndroidLogger.Instance.Log("Starting sending attempts");
        while (true)
        {
            Debug.Log("Sending to data to server");
            byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(command));
            udpSendClient.Send(sendBytes, sendBytes.Length);

            yield return new WaitForSeconds(updateTime);

        }
    }

    /// <summary>
    /// Main Thread variant of receiving udp data from the arduino.
    /// The arduinos code loop updates much faster than Unity's frame loop.
    /// This reduced polling rate in Unity can lead to missed packets, hence the Async version.
    /// Also this requires hte use of an IPEndPoint, just an extra thing.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator TryListenMainThread()
    {
        IPEndPoint remoteIPEndPoint = new(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                if (udpRecieveClient.Available > 0)
                {
                    byte[] receiveBytes = udpRecieveClient.Receive(ref remoteIPEndPoint);

                    string returnData = Encoding.ASCII.GetString(receiveBytes); // encode into the raw byte buffer to string

                    AndroidLogger.Instance.Log(returnData);
                    Debug.Log(returnData);

                    state = JsonUtility.FromJson<NerfgunState>(returnData); // parse the string to json
                }
            }
            catch (Exception e)
            {
                AndroidLogger.Instance.Log(e.ToString());
            }
            yield return null;
        }
    }

    /// <summary>
    /// This the asyncronous variant of the Try Listen coroutine.
    /// This runs hte receive function on a seperate thread which reduces the chance of missing a packet.
    /// This also increases performance slightly by running the task on a seperate core, on a mobile game,
    /// this is invaluable as performance is expensive but phones tend to have a lot of cores - even if they
    /// are slow as hell.
    /// 
    /// This method is set up to receive IMU data and was how i tested to see what remapping of the IMU quaternion was needed.
    /// After i figured it out her I made an implicit operator for converting between <see cref="IMUQuat"/> and <see cref="Quaternion"/>
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator TryListenAsync()
    {
        AndroidLogger.Instance.Log("Starting listener");
        Debug.Log("Starting listener");
        while (true) // forever, start a new ReceiveAsync task, wait till it completes.
        {
            var receive = udpRecieveClient.ReceiveAsync();

            // this while loop can be replaced with "yield return new WaitUntil(() => receiver.IsCompleted);"
            // at one point it had a logging message in it to see if the receiver was still alive.
            while (!receive.IsCompleted)
            {
                yield return null;
            }

            if (receive.IsFaulted) // if it fauled log the message and stop the coroutine
            {
                Debug.LogErrorFormat("Exception Caught! Message: {0}", receive.Exception.Message);
                AndroidLogger.Instance.Log(string.Format("Exception Caught! Message: {0}", receive.Exception.Message));
                yield break;
            }

            if (receive.IsCompletedSuccessfully) // if successful parse the data.
            {
                string returnData = Encoding.ASCII.GetString(receive.Result.Buffer);
                imu = JsonUtility.FromJson<IMUQuat>(returnData);
                imuEffectee.rotation = new Quaternion(imu.W, imu.X, imu.Z, imu.Y);
                eulerAngle = imuEffectee.rotation.eulerAngles;
            }
        }

    }

    /// <summary>
    /// close the sockets when disabled.
    /// </summary>
    private void OnDisable()
    {
        udpSendClient.Close();
        udpRecieveClient.Close();
    }
}
