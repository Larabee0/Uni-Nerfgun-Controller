using UnityEngine;

/// <summary>
/// Used to hold gyro calibration data in the PlayerPerfs class of unity between application runtimes.
/// This allows the calibration to be loaded at runtime and sent to the controller.
/// 
/// This class may appear in other submissions I have made though using XML instead of playerPerfs
/// </summary>
public class PersistantOptions : MonoBehaviour
{
    public GyroCalibrationSaveData gyroCalibrationData;

    public static PersistantOptions instance;

    public delegate void SettingsChanged();
    public SettingsChanged OnCalibrationChanged;

    /// <summary>
    /// on started see if the PlayerPers contains the GyroCalibration key and
    /// load it into the <see cref="GyroCalibrationSaveData"/> class
    /// </summary>
    private void Awake()
    {
        instance = this;
        gyroCalibrationData = new GyroCalibrationSaveData();
        if (PlayerPrefs.HasKey("GyroCalibration"))
        {
            gyroCalibrationData = JsonUtility.FromJson<GyroCalibrationSaveData>(PlayerPrefs.GetString("GyroCalibration"));   
        }
    }

    /// <summary>
    /// On application exit, save the perfs
    /// </summary>
    private void OnDestroy()
    {
        SaveCalibrationDataNow();
    }

    /// <summary>
    /// Method for saving the gyro calibration to player perfs
    /// Create an instance of <see cref="GyroCalibrationSaveData"/> if its null, serialize it to json and save it.
    /// </summary>
    public void SaveCalibrationDataNow()
    {
        gyroCalibrationData ??= new GyroCalibrationSaveData();

        string gyroJson = JsonUtility.ToJson(gyroCalibrationData);
        PlayerPrefs.SetString("GyroCalibration", gyroJson);
    }
}

/// <summary>
/// Save data for gyro calibration.
/// </summary>
public class GyroCalibrationSaveData
{
    public bool calibratedGyro = false;
    public int CX; // gyro X offset
    public int CY; // gyro Y offset
    public int CZ; // gyro Z offset
    public Vector4 scopeBorders = new(25, 25, 25, 25);

    /// <summary>
    /// Stores the relavent data from <see cref="NerfgunCommandRequest"/>
    /// It will contain calibration data. Save it when stored.
    /// </summary>
    /// <param name="calibartionData"></param>
    public void SetCalibrationData(NerfgunCommandRequest calibartionData)
    {
        calibratedGyro = true;
        CX = calibartionData.CX;
        CY = calibartionData.CY;
        CZ = calibartionData.CZ;
        PersistantOptions.instance.SaveCalibrationDataNow();
    }

    /// <summary>
    /// Loads the current instance into the given commands,
    /// sets the CG flag to 2 to tell the controller to load the calibrated data.
    /// the commands will then be sent up to the gun by the caller.
    /// </summary>
    /// <param name="commands">Commands for the physical nerfgun</param>
    /// <returns>Commands with the gyro calibration data and CG flag</returns>
    public NerfgunCommandRequest ApplyCalibrationData(NerfgunCommandRequest commands)
    {
        commands.CG = 2;
        commands.CX = CX;
        commands.CY = CY;
        commands.CZ = CZ;
        return commands;
    }
}
