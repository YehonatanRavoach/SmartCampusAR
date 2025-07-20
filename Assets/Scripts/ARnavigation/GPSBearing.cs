using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using UnityEngine.XR.ARFoundation;

/******************************************************************************
 * GPSBearing.cs
 * -----------------------------------------------------------------------------
 * AR navigation helper that:
 *   • Starts the device GPS + compass and waits for a valid fix.
 *   • Shows a calibration panel until horizontal + heading accuracy are within
 *     desired thresholds (configurable in Inspector).
 *   • Tracks current user latitude/longitude continuously.
 *   • When given a target lat/lon (via SetTarget), computes:
 *        - Distance to target   (meters)
 *        - Estimated minutes to arrival (using `speed` m/s ⇒ min)
 *        - Estimated arrival clock time
 *        - Bearing from current position to target
 *   • Rotates 3D compass visuals to reflect device heading.
 *   • Rotates an AR 3D arrow (`redarrow3d`) to point toward the target bearing
 *     relative to the device camera heading (smoothly filtered).
 *   • Detects arrival within `arriveThreshold` meters → shows modal, hides arrow.
 *
 *  Usage
 *  -----
 *   1. Place this script in your AR scene.
 *   2. Assign references in the Inspector:
 *        arCamera, compass3d, redarrow3d, UI TMP fields, calibrationPanel…
 *   3. Call `SetTarget(lat, lon)` when you want to navigate.
 *   4. `StopNavigation()` to cancel.
 *
 *  Notes
 *  -----
 *   • Location & compass permissions must be granted at OS level.
 *   • `speed` is user walking speed estimate (m/s) for ETA; adjust as needed.
 *   • `headingSmoothFactor` controls low-pass smoothing of the compass heading.
 *   • This script relies on Unity’s built-in `Input.location` & `Input.compass`.

 *****************************************************************************/

public class GPSBearing : MonoBehaviour
{
    public Camera arCamera; // drag AR Camera here (or the object that has the camera)
    readonly Quaternion modelOffset = Quaternion.identity;  // no offset
    public ARPlaneManager planeManager;   // drag AR Session Origin (or the object that has the manager)
    public Material replacement; // Material to replace the plane mesh with
    float speed = 1.4f; // Average walking speed in meters per second (1.4 m/s ≈ 5 km/h)

    public GameObject modalBackground; // Modal background to show when user arrives at target
    float arriveThreshold = 3f; // How close to the target before we consider it "arrived" (in meters)
    public bool isNavigating = false; // Are we currently navigating to a location?

    public TMP_Text show_Time;
    public TMP_Text timeleft_Text;
    public TMP_Text debugTxt;
    public TMP_Text displayDistance;
    GPSLoc currLoc = new GPSLoc();// current GPS location
    public double CurrentLat => currLoc.lat;   // latitude in degrees
    public double CurrentLon => currLoc.lon;   // longitude in degrees

    public GameObject compass2d;
    

    public GameObject compass3d; // 3D compass model to rotate based on device heading
    public GameObject redarrow3d;// 3D arrow model to point toward the target bearing

    [Header("Prefabs & Containers")]
    private double targetLat;
    private double targetLon;
    public bool gps_ok = false; 

    public static bool CalibrationDone { get; set; } // 🔹 static flag to indicate calibration is done
    public bool once = false;

    float timeDelay = 0.25f; //delay update, reduces jitter

    float magNorth = 0;

    private float smoothedHeading = 0f; // Smoothed compass heading in degrees
    public float headingSmoothFactor = 0.2f; // 0.2f = 20% smoothing, 0.5f = 50% smoothing, etc.

    [Header("Calibration UI")]

    public GameObject calibrationPanel; // Panel to show during calibration

    public float desiredHorizontalAccuracy = 5f; // desired horizontal accuracy in meters

    public float desiredHeadingAccuracy = 15f; // desired compass heading accuracy in degrees
    float accuracy = 0f; // Compass heading accuracy in degrees

    IEnumerator Start()
    {

        // Check if the user has location service enabled.
        if (!Input.location.isEnabledByUser)
        {
            Debug.Log("Location not enabled on device or app does not have permission to access location");
            debugTxt.text = "Location not enabled on device or app does not have permission to access location";
        }
        // Starts the location service.
        Input.location.Start();
        Input.compass.enabled = true;


        // Waits until the location service initializes
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // If the service didn't initialize in 20 seconds this cancels location service use.
        if (maxWait < 1)
        {
            Debug.Log("Timed out");
            debugTxt.text += "\nTimed Out";
            yield break;
        }
        redarrow3d.transform.localScale = Vector3.one * 0.2f;
        // If the connection failed this cancels location service use.
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogError("Unable to determine device location");
            debugTxt.text += ("\nUnable to determine device location");

            yield break;

        }
        else
        {
            // If the connection succeeded, this retrieves the device's current location and displays it in the Console window.
            Debug.Log("Location: " + Input.location.lastData.latitude + " " + Input.location.lastData.longitude + " " + Input.location.lastData.altitude + " " + Input.location.lastData.horizontalAccuracy + " " + Input.location.lastData.timestamp);
            debugTxt.text
                = "\nLocation: \nLat: " + Input.location.lastData.latitude
                + " \nLon: " + Input.location.lastData.longitude
                + " \nAlt: " + Input.location.lastData.altitude
                + " \nH_Acc: " + Input.location.lastData.horizontalAccuracy
                + " \nTime: " + Input.location.lastData.timestamp;


            gps_ok = true;
        }

        if (calibrationPanel != null)
            calibrationPanel.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {

        if (!gps_ok)
            return;

        if (GPSBearing.CalibrationDone && calibrationPanel && calibrationPanel.activeSelf)
            calibrationPanel.SetActive(false);

        // Check if the location service is enabled and has a valid location
        float horizAcc = Input.location.lastData.horizontalAccuracy;
        float compassAcc = Input.compass.headingAccuracy;
        if (!once)
        {
            if (horizAcc > desiredHorizontalAccuracy || compassAcc > desiredHeadingAccuracy)
            {
                if (calibrationPanel && !calibrationPanel.activeSelf)
                    calibrationPanel.SetActive(true);
                debugTxt.text = $"Calibrating…\nGPS accuracy: {horizAcc:F1} m (≤ {desiredHorizontalAccuracy} m)\nCompass accuracy: {compassAcc:F1}° (≤ {desiredHeadingAccuracy}°)";
                return;
            }
            else
            {
                if (calibrationPanel && calibrationPanel.activeSelf)
                    calibrationPanel.SetActive(false);
                GPSBearing.CalibrationDone = true;   // 🔹 announce “calibrated”
                once = true;                         // 🔹 so we don't test again
            }
        }


        if (gps_ok)
        {

            debugTxt.text = "GPS:...";

            debugTxt.text
                = "\nLocation: \nLat: " + Input.location.lastData.latitude
                + " \nLon: " + Input.location.lastData.longitude
                + " \nH_Acc: " + Input.location.lastData.horizontalAccuracy;


            currLoc.lat = Input.location.lastData.latitude;
            currLoc.lon = Input.location.lastData.longitude;


            //get compass data
            timeDelay -= Time.deltaTime; //update every 1/4 second - reduces jitter, could average instead? 
            if (timeDelay < 0)
            {
                timeDelay = 0.15f; //reset timer
                float rawHeading = Input.compass.trueHeading;
                if (Mathf.Approximately(smoothedHeading, 0f) && rawHeading > 0f)
                {
                    smoothedHeading = rawHeading;
                }
                else
                {
                    // Use Mathf.DeltaAngle so we smoothly handle wrap at 360→0
                    float delta = Mathf.DeltaAngle(smoothedHeading, rawHeading);
                    smoothedHeading += delta * headingSmoothFactor;
                }
                magNorth = Input.compass.magneticHeading;
                accuracy = Input.compass.headingAccuracy;
                if (accuracy > 15f)
                {
                    // The reading is unreliable. Optionally skip or reduce smoothing factor further
                    debugTxt.text += "\nCompass accuracy is poor! Try calibrating.";
                }

                //update UI 
                compass3d.transform.localEulerAngles = new Vector3(smoothedHeading - 135, 0, 0);
                compass2d.transform.localEulerAngles = new Vector3(0, 0, smoothedHeading);

            }
            if (isNavigating)
            {

                //Distance to target from current location
                double distanceBetween = distance((double)currLoc.lat, (double)currLoc.lon, targetLat, targetLon);
                debugTxt.text += "\nDistance: " + distanceBetween;
                displayDistance.text = distanceBetween.ToString("F2") + " m";
                debugTxt.text += "\nSaved Lat: " + targetLat;
                debugTxt.text += "\nSaved Lon: " + targetLon;

                float timeLeft = (float)distanceBetween / speed / 60f;
                int timeRounded = Mathf.CeilToInt(timeLeft);
                timeleft_Text.text = timeRounded + " min";

                DateTime now = DateTime.Now;
                DateTime arrivalTime = now.AddMinutes(timeRounded);
                string arrivaltime_String = arrivalTime.ToString("HH:mm");
                show_Time.text = $"{arrivaltime_String}";

                //Get the bearing to the target
                double bearing = getBearing((double)currLoc.lat, (double)currLoc.lon, targetLat, targetLon);

                debugTxt.text += "\nBearing to Target " + bearing;



                // -----------------------------------------------------------
                // A) yaw DIFFERENCE between where we need to go (bearing)
                //    and where the phone is pointing (smoothedHeading)
                float yawDelta = Mathf.DeltaAngle(smoothedHeading, (float)bearing); // −180…+180

                // B) convert that relative turn into an absolute world-space yaw
                float cameraYaw = arCamera.transform.eulerAngles.y;   // current world yaw
                float targetYaw = cameraYaw + yawDelta;

                // optional: 90 / −90 / 180 if your mesh does not point +Z forward
                const float modelYawOffset = 0f;
                targetYaw += modelYawOffset;

                // C) smooth and apply (no roll)
                float currentYaw = redarrow3d.transform.eulerAngles.y;
                float smoothYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * 5f);

                redarrow3d.transform.rotation = Quaternion.Euler(0f, smoothYaw, 90f);


                //Check if user arrived
                if (distanceBetween <= arriveThreshold)
                {
                    var mover = FindObjectOfType<ArrowMover>();
                    if (mover) mover.HideArrow();

                    GPSBearing.CalibrationDone = false;   // prevent re-spawn
                    ShowArrivedModal();
                }

            }


            //output compass stuff
            debugTxt.text += "\nMag North " + magNorth;
            debugTxt.text += "\nTrue North: " + smoothedHeading;
            debugTxt.text += "\nHeadAcc: " + accuracy;
        }

    }


    public void SetTarget(double lat, double lon) // Set the target latitude and longitude for navigation
    {
        targetLat = lat;
        targetLon = lon;
        Debug.Log("Target lat:" + targetLat);
        Debug.Log("Target lon:" + targetLon);

        isNavigating = true;
    }

    double getBearing(double lat1, double lon1, double lat2, double lon2)  // Calculate the bearing from point 1 to point 2
    {

        lat1 = lat1 * Math.PI / 180.0;
        lon1 = lon1 * Math.PI / 180.0;
        lat2 = lat2 * Math.PI / 180.0;
        lon2 = lon2 * Math.PI / 180.0;

        double deltaLon = lon2 - lon1;

        double y = Math.Sin(deltaLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(deltaLon);
        double bearingRad = Math.Atan2(y, x);

        // Convert to degrees
        double bearingDeg = bearingRad * 180.0 / Math.PI;
        // Normalize to [0..360)
        bearingDeg = (bearingDeg + 360.0) % 360.0;

        return bearingDeg;
    }



    //https://www.geodatasource.com/resources/tutorials/how-to-calculate-the-distance-between-2-locations-using-c/
    private double distance(double lat1, double lon1, double lat2, double lon2)
    {
        if ((lat1 == lat2) && (lon1 == lon2))
        {
            return 0;
        }
        else
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            dist = dist * 1609.344;
            return (dist);
        }
    }


    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //::  This function converts decimal degrees to radians             :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private double deg2rad(double deg)
    {
        return (deg * Math.PI / 180.0);
    }

    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    //::  This function converts radians to decimal degrees             :::
    //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
    private double rad2deg(double rad)
    {
        return (rad / Math.PI * 180.0);
    }

    public void changeArrow(GameObject arrow)
    {
        redarrow3d = arrow;
    }

    void ShowArrivedModal()
    {
        // 1. Stop navigation logic
        isNavigating = false;
        // 2. Show the modal
        modalBackground.SetActive(true);
    }

    public void ChangePlanePrefab()
    {
        var mr = planeManager.planePrefab.GetComponentInChildren<MeshRenderer>();
        if (mr) mr.sharedMaterial = replacement;
    }

    public void StopNavigation()
    {
        isNavigating = false;                 // stop distance/ETA update
        once = false;                // force re-run of calibration check
        CalibrationDone = false;              // ArrowMover will now wait
                                              // for the next good fix
                                              // hide arrow immediately
        var mover = FindObjectOfType<ArrowMover>();
        if (mover) mover.HideArrow();
    }

}



public class GPSLoc
{
    public float lon;
    public float lat;

    public GPSLoc()
    {
        lon = 0;
        lat = 0;
    }
    public GPSLoc(float lon, float lat)
    {
        this.lon = lon;
        this.lat = lat;
    }

    public string getLocData()
    {
        return "Lat: " + lat + " \nLon: " + lon;
    }

    
}