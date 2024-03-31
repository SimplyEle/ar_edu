using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARController : MonoBehaviour
{
    public void StopScanning()
    {
        GetComponent<ARPlaneManager>().detectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.None;
    }
}
