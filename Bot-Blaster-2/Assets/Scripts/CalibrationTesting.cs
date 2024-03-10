using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class I used for testing gyroscope calibration - commanding the gun to enter calibration mode &
/// commanding the gun to receive a calibration.
/// 
/// Because of hardware limitations and network socket limitations sending calibration data is complicated.
/// The ESP8266 only recieves packets on port 5000
/// The Network manager on windows can only have one open udp socket on that port so <see cref="GunCommunication"/> sender client must send the packet
/// even though this is more of a <see cref="GyroscopeCommuincation"/> function.
/// 
/// But things get far more complicated when receiving calibration data. The two classes become inheriently intertwined .
/// 
/// The two classe swap their udpclients so the <see cref="GyroscopeCommuincation"/> can receive on the port <see cref="GunCommunication"/>
/// this is because on the esp8266 side, only gyroscope rotation data is sent on <see cref="GyroscopeCommuincation.receiverPort"/>
/// Ideally I would shut down the clients and then reopen them as desired however this is not possible due to socket timeout
/// limitations (you need to wait a certain amount of time after closing a socket to reopen it. This solution is something
/// that can be done right now in one method call.
/// 
/// </summary>
public class CalibrationTesting : MonoBehaviour
{
    [SerializeField] public Text outputText;

    [SerializeField] private GyroscopeCommuincation gyroscopeCommuincation;
    [SerializeField] private GunCommunication gunCommuincation;

    [SerializeField] float waitTime = 30f;

    void Start()
    {
        gyroscopeCommuincation = GetComponent<GyroscopeCommuincation>();
        gunCommuincation = GetComponent<GunCommunication>();
        StartCoroutine(RunSendCalibration());
        //StartCoroutine(RunTimeCalibration());
    }

    /// <summary>
    /// waits the duration then calls SendGyro Calibration <see cref="GyroscopeCommuincation.SendGyroCalibration"/>
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator RunSendCalibration()
    {
        for (float i = 0; i < waitTime; i += Time.deltaTime)
        {
            outputText.text = string.Format("Begining Send Calibration in: {0}", ((int)(waitTime - i)).ToString());
            yield return null;
        }
        gyroscopeCommuincation.SendGyroCalibration();
    }

    /// <summary>
    /// Waits the duration then commands the gun to calibrate and return data <see cref="GyroscopeCommuincation.BeginReceiveGyroCalibration"/>
    /// This also waits until the calibration data is received and logs the data recived and how log it took to the android logger.
    /// </summary>
    /// <returns>Coroutine Enumerator</returns>
    private IEnumerator RunTimeCalibration()
    {
        for (float i = 0; i < waitTime; i+=Time.deltaTime)
        {
            AndroidLogger.Instance.Log(string.Format("Move gun in figure-8 until calibration completed\nBegining Calibrating in: {0}", ((int)(waitTime - i)).ToString()));
            yield return null;
        }
        gyroscopeCommuincation.BeginReceiveGyroCalibration();

        float calibrationTime = 0;
        while (true)
        {
            if(gyroscopeCommuincation.Calibrating)
            {
                AndroidLogger.Instance.Log(string.Format("Move gun in figure-8 until calibration completed\nCalibrating... waited: {0}s", ((int)calibrationTime).ToString()));
            }
            else
            {
                AndroidLogger.Instance.Log(string.Format("Calibrated"));
                yield break;
            }
            calibrationTime += Time.deltaTime;
            yield return null;
        }
    }
}
