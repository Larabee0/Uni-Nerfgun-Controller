using System;
using UnityEngine;

// just a delegate to notify a thing to do a thing. seemed like a good place for it to live.
public delegate void Pulse();

// none of these json classes are actually classes.

[Serializable]
public struct NerfgunState
{
    // 0, single shot
    // 1, burst shot (3 darts)
    // 2, full auto
    public byte RF; // rate of fire 0, 1 and 2
    public bool A1; // ammo present
    public bool A2; // door open
    public bool A3; // mag inserted
    public bool S1; // fly wheel trigger
    public bool S2; // main trigger
    public bool RB; // rotary encoder button (reset scope zoom)
    public int encoderPos; // rotary encoder count (can be negative)

    public bool BurstMode => RF != 2;
    public bool BurstLockOut(int shotsFired)
    {
        return !BurstMode || (RF == 0 && shotsFired < 1) || (RF == 1 && shotsFired < 3);
    }
}

/// <summary>
/// used to send the gun commands and also gyro calibration data/mode
/// </summary>
[Serializable]
public struct NerfgunCommandRequest
{
    public byte EM; // emulate nerfgun (0 or 1)
    // CG = 0 ESP8266 should just send imuQuat
    // CG = 1 ESP8266 should enter compass calibration mode
    // CG = 2 game is sending calibration data to ESP8266
    // CG = 3 game should save calibration data
    public byte CG;
    public int CX; // gyro X offset
    public int CY; // gyro Y offset
    public int CZ; // gyro Z offset

}

[Serializable]
public struct IMUQuat 
{
    public float W;
    public float X;
    public float Y;
    public float Z;

    /// <summary>
    /// Just lets me do:
    /// 
    /// IMUQuat imuRot = *parse from json*;
    /// Quaternion objectRotation = imuRot;
    /// 
    /// </summary>
    /// <param name="q">Raw IMU Quaternion from ESP8266</param>
    public static implicit operator Quaternion(IMUQuat q) => new(q.W, q.X, q.Z, q.Y);
}
