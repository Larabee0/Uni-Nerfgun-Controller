using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple script for observing frame timing stats on android
/// </summary>
public class FPSCounter : MonoBehaviour
{
    [SerializeField] private Text fpsText;
    
    private void Update()
    {
        fpsText.text = string.Format("tFR: {1} dT: {0}ms\nfdT: {2}ms", (Time.deltaTime * 1000f).ToString("00.00"),Application.targetFrameRate,Time.fixedDeltaTime.ToString("00.00"));
    }
}
