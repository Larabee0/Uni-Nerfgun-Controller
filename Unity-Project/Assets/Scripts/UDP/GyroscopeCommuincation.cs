using System.Collections;
using UnityEngine;
using System.Text;
using System;
//using UnityEngine.UI;

/// <summary>
/// Class for handling most communication from the ESP8266 about hte gyroscope.
/// 
/// The primary purpose is to receive gryo data and rotate the virtual character to relfect the physical orientation of the player.
/// 
/// The secondary purpose is to provide the infrastructure to send gyro calibration data from the game to the gun.
/// 
/// The tertiary purpose to command the gun to calibrate itself then send the calibration data back to the game.
/// 
/// these latter two functions are quite complicated due to limitations with UDP sockets & the ESP8266.
/// </summary>
public class GyroscopeCommuincation : UDPCommuincator
{
    [SerializeField] private GunCommunication gunCommuincation;
    //[SerializeField] private Button calibrateButton;

    [SerializeField] private bool gyroCalibrationMode = false;
    public bool Calibrating => gyroCalibrationMode;

    
    private Quaternion gyroScopeOffsetRotation; // functionality to offset the quaternion sent back to us
    [SerializeField] private Vector3 gyroScopeOffsetEuler = Vector3.zero;

    [SerializeField] private Quaternion remappedGyroQuaternion = Quaternion.identity;


#if UNITY_EDITOR
    [SerializeField] private Vector3 gyroEuler;
    [SerializeField] private bool editorOverrides;
    [SerializeField] private float mouseSensitivty = 1f;
    private Vector3 mousePosLastFrame;
#endif

    /// <summary>
    /// Provision for testing in the edtior with keyboard and mouse.
    /// During Android runtime this just starts the receiver client and sets the gyroScopeOffsetRotation
    /// </summary>
    private void OnEnable()
    {
#if UNITY_EDITOR
        //Cursor.lockState = CursorLockMode.Locked;
        if (!editorOverrides)
        {
            StartReceiverClient(receiverPort);
        }
#else
        StartReceiverClient(receiverPort);
#endif
        gyroScopeOffsetRotation = Quaternion.Euler(gyroScopeOffsetEuler);
    }

    /// <summary>
    /// Determines whether the gun should be calibrated then the data send back
    /// or whether we should send it calibration data.
    /// </summary>
    private void Start()
    {
#if PLATFORM_ANDROID
        if (PersistantOptions.instance.gyroCalibrationData.calibratedGyro)
        {
            SendGyroCalibration();
        }
        else
        {
            BeginReceiveGyroCalibration();
        }
#endif
    }

    /// <summary>
    /// Editor overrides for keyobard and mouse support.
    /// This is a terrible way of moving the camera but its good enough
    /// </summary>
#if UNITY_EDITOR
    private void Update()
    {
        if(editorOverrides)
        {
            float mouseX = Input.GetAxisRaw("Mouse X");
            float mouseY = -Input.GetAxisRaw("Mouse Y");
            transform.Rotate(new Vector3(mouseY, mouseX),Space.Self);
        }
    }
#endif

    /// <summary>
    /// Either rotates the virtual character or tries to recieve the calibration data
    /// </summary>
    /// <param name="recievedData">Data recieved by the UDP client</param>
    protected override void OnPacketReceived(string recievedData)
    {
        if (gyroCalibrationMode)
        {
            TrySetGyroCalibration(recievedData);
        }
        else
        {
            SetGunOreintation(recievedData);
        }
    }

    /// <summary>
    /// Rotates the virtual character to match the physical orientation.
    /// </summary>
    /// <param name="recievedData">Physical Orienation data formatted as <see cref="IMUQuat"/> JSON</param>
    private void SetGunOreintation(string recievedData)
    {
        
        remappedGyroQuaternion = JsonUtility.FromJson<IMUQuat>(recievedData);

        remappedGyroQuaternion *= gyroScopeOffsetRotation; // offset rotation
        transform.rotation = remappedGyroQuaternion;

#if UNITY_EDITOR
        gyroEuler = transform.rotation.eulerAngles;
#endif
    }

    /// <summary>
    /// When in gyro calibration mode, whether a UDP packet is recieved this method is called.
    /// if it is able to parse the json to <see cref="NerfgunCommandRequest"/> and CG = 3,
    /// it is determined that the calibration data is ready. we save it, swap back the UDP clients and resume normal operations
    /// </summary>
    /// <param name="recievedData">UDP Packet hopefully in <see cref="NerfgunCommandRequest"/>JSON format</param>
    private void TrySetGyroCalibration(string recievedData)
    {
        NerfgunCommandRequest command = JsonUtility.FromJson<NerfgunCommandRequest>(recievedData);
        if (command.CG != 3)
        {
            return;
        }

        gyroCalibrationMode = false;
        command.CG = 0;
        gunCommuincation.Command = command;

        /// we earlier swapped the UDP clients between <see cref="gunCommuincation"/> and <see cref="this"/>
        /// this should now be swapped back for normal operations to resume.
        (udpRecieverClient, gunCommuincation.udpRecieverClient) = (gunCommuincation.udpRecieverClient, udpRecieverClient);
        
        // fire a command back to the gun now command.CG = 0
        // this indicates that it should start sending gyro orientation data.
        gunCommuincation.SendGunCommand();
        PersistantOptions.instance.gyroCalibrationData.SetCalibrationData(command);
        UIController.Instance.AllowCalibration(true);
    }

    /// <summary>
    /// This commands the gun to enter calibration mode and return the calibration data to the game when its finished.
    /// This method commands the gun to do it and prepares <see cref="gunCommuincation"/> and <see cref="this"/> scripts
    /// for the operation.
    /// </summary>
    public void BeginReceiveGyroCalibration()
    {
        UIController.Instance.AllowCalibration(false);
        NerfgunCommandRequest commands = gunCommuincation.Command;
        commands.CG = 1;
        gunCommuincation.Command = commands;
        gunCommuincation.SendGunCommand();
        gyroCalibrationMode = true;

        /// calibration data is received on the the port used by <see cref="gunCommuincation"/>
        /// we cannot close and reopen the sockets due to limitations with the systems UDP sockets,
        /// instead we swap the clients between the scripts for hte duration of the calibration.
        (gunCommuincation.udpRecieverClient, udpRecieverClient) = (udpRecieverClient, gunCommuincation.udpRecieverClient);
    }

    /// <summary>
    /// loads and then commands a send request of gyro calubration data. No interruption of data is expected.
    /// </summary>
    public void SendGyroCalibration()
    {
        NerfgunCommandRequest commands = gunCommuincation.Command;
        gunCommuincation.Command = PersistantOptions.instance.gyroCalibrationData.ApplyCalibrationData(commands);
        gunCommuincation.SendGunCommand();
    }
}
