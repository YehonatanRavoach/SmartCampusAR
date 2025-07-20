using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/******************************************************************************
 * GPSObjectSpawner.cs
 * -----------------------------------------------------------------------------
 * Spawns a world‑space “info display” prefab when the user’s real‑time GPS
 * location is within <see cref="activationRadius"/> metres of any mock point
 * defined in <see cref="mockPoints"/>.  
 *
 *  Workflow per frame
 *  ------------------
 *  1. Ensure `Input.location` is running; bail out otherwise.  
 *  2. Loop over all <see cref="ARMockInfo"/> entries that have **not** yet been
 *     instantiated:  
 *      • Compute Haversine distance to the user.  
 *      • If ≤ radius → cast a ray from the screen‑centre into AR planes
 *        (via <see cref="ARRaycastManager"/>).  
 *      • On a hit, <see cref="Instantiate"/> `objectToPlace`, call
 *        <see cref="InfoDisplay.SetInfo"/> (if present), and mark that index
 *        as spawned so it won’t spawn again.  
 *
 *  Public Inspector Fields
 *  -----------------------
 * • **mockPoints**           List of GPS coords + mock data (type, value).  
 * • **objectToPlace**        Prefab that contains an `InfoDisplay` script.  
 * • **raycastManager**       Reference to the scene’s ARRaycastManager.  
 * • **activationRadius**     Meters within which to spawn.  
 *****************************************************************************/

[System.Serializable]
public class ARMockInfo
{
    public double lon;
    public double lat;
    public string type = "Temperature";
    public float data = 25f;
}



public class GPSObjectSpawner : MonoBehaviour
{
    [Header("Mock Points List !")]
    public List<ARMockInfo> mockPoints = new List<ARMockInfo>();

    [Header("Placement")]
    [Tooltip("Prefab InfoDisplay + Canvas World-Space")]
    public GameObject objectToPlace;
    public ARRaycastManager raycastManager;
    public float activationRadius = 5f;


    private readonly HashSet<int> spawnedIndexes = new HashSet<int>();

    void Start()
    {

    }



    void Update()
    {
        if (Input.location.status != LocationServiceStatus.Running)
            return;

        double userLat = Input.location.lastData.latitude;
        double userLon = Input.location.lastData.longitude;


        if (mockPoints.Count == 0)
        {
            Debug.LogWarning("mockPoints list is EMPTY Inspector!");
            return;
        }

        for (int i = 0; i < mockPoints.Count; i++)
        {
            if (spawnedIndexes.Contains(i)) continue;

            ARMockInfo p = mockPoints[i]; // Get the mock point
            float dist = GetDistanceMeters(userLat, userLon, p.lat, p.lon); // Calculate distance in meters
            Debug.Log($"Point {i}: {dist:F1} m away");

            if (dist <= activationRadius)
            {
                TryPlaceObject(p);// Try to place the object
                if (TryPlaceObject(p))
                    spawnedIndexes.Add(i); // Mark as spawned

            }
        }
    }
    // Calculates the Haversine distance between two GPS coordinates in meters
    
    float GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const float R = 6371000f;
        float dLat = Mathf.Deg2Rad * ((float)lat2 - (float)lat1);
        float dLon = Mathf.Deg2Rad * ((float)lon2 - (float)lon1);
        float a = Mathf.Sin(dLat * 0.5f) * Mathf.Sin(dLat * 0.5f) +
                  Mathf.Cos(Mathf.Deg2Rad * (float)lat1) * Mathf.Cos(Mathf.Deg2Rad * (float)lat2) *
                  Mathf.Sin(dLon * 0.5f) * Mathf.Sin(dLon * 0.5f);
        return 2f * R * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1f - a));
    }

    // Tries to place the object at the screen center on a detected AR plane
    // Returns true if placement was successful, false otherwise.
    bool TryPlaceObject(ARMockInfo info)
    {
        List<ARRaycastHit> hits = new();
        Vector2 center = new(Screen.width * 0.5f, Screen.height * 0.5f);


        if (!raycastManager.Raycast(center,
                                    hits,
                                    UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("No plane detected – skip placement this frame.");
            return false;
        }

        Pose pose = hits[0].pose;
        var go = Instantiate(objectToPlace, pose.position, pose.rotation);

        if (go.TryGetComponent(out InfoDisplay disp))
            disp.SetInfo(info.type, info.data);

        Debug.Log("Placed AR object on detected plane.");
        return true;
    }

}
