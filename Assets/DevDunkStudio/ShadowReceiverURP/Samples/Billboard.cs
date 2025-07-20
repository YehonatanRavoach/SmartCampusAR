using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;    
using System.Collections.Generic;     

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main)
            transform.LookAt(Camera.main.transform, Vector3.up);
    }
}