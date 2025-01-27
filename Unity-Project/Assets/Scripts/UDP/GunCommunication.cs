using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.UI;

/// <summary>
/// Class used to handling communication with the physical nerfgun. This receives state changes from the ESP8266
/// and can also send a CommandRequest back to command the gun to do basic operations.
/// </summary>
public class GunCommunication : UDPCommuincator
{
    //public Toggle nerfgunEmulationToggle;
    [SerializeField] private NerfgunState state;
    public NerfgunState State => state;

    [SerializeField] private NerfgunCommandRequest command;
    public NerfgunCommandRequest Command { get => command; set => command = value; }

    public Action OnNerfgunFired;

    private int physicalShotsFired = 0;
    public int PhysicalShotsfired => physicalShotsFired;

    private bool physicalGunMode = false;
    public bool PhysicalGunMode => physicalGunMode;

    /// <summary>
    /// Start the receiver and sender client <see cref="UDPCommuincator"/>
    /// </summary>
    private void OnEnable()
    {
        StartReceiverClient(receiverPort);
        StartSenderClient();
    }
    
    /// <summary>
    /// called whether the nerfgun state has changed. If the string length is less than,
    /// 30 we determine this to be the nerfgun informing us it has fired a shot.
    /// </summary>
    /// <param name="recievedData">UDP data</param>
    protected override void OnPacketReceived(string recievedData)
    {
        if(recievedData.Length < 30)
        {
            physicalShotsFired += 1;
            OnNerfgunFired?.Invoke();
            AndroidLogger.Instance.Log(string.Format("Shots Fired: {0}", PhysicalShotsfired));
            return;
        }
        state = JsonUtility.FromJson<NerfgunState>(recievedData);
    }

    /// <summary>
    /// Serializes the commands and sends them to the gun. <see cref="NerfgunCommandRequest"/>
    /// Also makes sure the toggle on screen is in the correct state
    /// </summary>
    public void SendGunCommand()
    {
        //nerfgunEmulationToggle.isOn = command.EM == 1;
        UIController.Instance.UpdateFiringMode(command.EM == 1);
        SendPacket(JsonUtility.ToJson(command));
    }

    /// <summary>
    /// When the emulate toggle box value changes it calls this method
    /// This method sets the mode of the nerfgun, should it fire for real or just virtually?
    /// </summary>
    public void SetNerfGunEmulationMode()
    {
        //SetNerfGunEmulationMode(nerfgunEmulationToggle.isOn);
    }

    public void SetNerfGunEmulationMode(bool enabled)
    {
        physicalGunMode = enabled;

        NerfgunCommandRequest commands = command;
        commands.EM = (byte)(physicalGunMode ? 1 : 0);
        command = commands;
        SendGunCommand();
    }
}
