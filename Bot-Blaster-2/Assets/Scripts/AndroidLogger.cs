using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// simple class to write to a text box on screen to work as a debug console on android
/// </summary>
public class AndroidLogger : MonoBehaviour
{
    public static AndroidLogger Instance { get; private set; }

    //[SerializeField] private Text logger;

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        Application.targetFrameRate = 120;
    }
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    public void Log(string message)
    {
        //logger.text = message;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {

        if (type == LogType.Error)
        {
            Log( logString);
        }
    }
}
