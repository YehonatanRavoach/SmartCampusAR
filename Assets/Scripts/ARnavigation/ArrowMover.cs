using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections;
/******************************************************************************
 * ArrowMover.cs
 * -----------------------------------------------------------------------------
 * Glides an AR “direction arrow” (redarrow3d) onto the first suitable ground
 * plane in front of the user and keeps its position smoothly updated.
 *
 *  Workflow
 *  --------
 *  1.  Waits until <see cref="GPSBearing.CalibrationDone"/> becomes true.
 *  2.  Each frame:
 *        • Casts a ray straight down from a point `probeDistance` metres ahead
 *          of the camera to look for **horizontal** planes that exceed
 *          `minPlaneSize` (square metres).  
 *        • Once a valid plane is found, stores the hit‑point (plus optional
 *          `heightOffset`) as `currentTarget`.  
 *        • Smooth‑interpolates the arrow’s world‑position toward that target
 *          using an exponential factor derived from `smoothing`.  
 *  3.  Shows a “coaching” UI panel while no plane has been accepted; hides
 *      the panel the moment the arrow first appears.  
 *
 *  Extras
 *  ------
 *  • After `swapDelay` seconds the script replaces ARPlane dotted debug
 *    materials with a transparent “shadows‑only” material (`shadowMaterial`)
 *    both for **existing** planes and for the plane prefab so that future
 *    detections are also transparent.  
 *  • Public <see cref="HideArrow"/> stops tracking, hides the arrow & panel,
 *    and resets <c>GPSBearing.CalibrationDone</c> so the flow restarts.  
 *  • <see cref="changeArrow(Transform)"/> lets you swap the Transform reference
 *    at runtime (theme/skin switch).  
 *****************************************************************************/




/// Glides the arrow onto the first large, low plane that is detected.
/// Shows a coaching panel while searching, hides it when the arrow appears.
[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class ArrowMover : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Camera arCamera;
    [SerializeField] Transform arrow;               // redarrow3d pivot
    [SerializeField] GameObject coachingUI;   // 🔵 add this

    [Header("Behaviour")]
    [SerializeField] float probeDistance = 3f;    // metres ahead
    [SerializeField] float heightOffset = 0.02f; // hover if >0
    [SerializeField] float smoothing = 0.10f; // 0-snappy, 1-laggy
    [SerializeField] float minPlaneSize = 1.0f;  // 1 m² threshold

    [Header("Plane-material swap")]                // ◆
    [SerializeField] Material shadowMaterial;      // ◆ assign TransparentWithShadows 
    [SerializeField] float swapDelay = 5f;      // ◆ seconds to wait

    ARRaycastManager raycaster;
    ARPlaneManager planeManager;
    static readonly List<ARRaycastHit> hits = new();

    bool arrowPlaced;
    Vector3 currentTarget;      // where the arrow is/was
    public static bool ArrowPlaced { get; private set; }

    void Awake()
    {
        raycaster = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();

        // We only care about horizontal ground planes
        planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;

        // Hide arrow & show coaching at startup
        if (arrow) arrow.gameObject.SetActive(false);
        if (coachingUI) coachingUI.SetActive(true);     // 🔵
        StartCoroutine(SwapPlanesAfterDelay());    // ◆
    }

    // Called every frame, after Update()
    void LateUpdate()
    {

        if (!GPSBearing.CalibrationDone)
            return;               // skip everything until calibration complete
        
        
        if (!arrow) return;

        // ------------ 1. Try to find a plane in front ------------
        Vector3 flatDir = Vector3.ProjectOnPlane(arCamera.transform.forward,
                                                 Vector3.up).normalized;
        if (flatDir == Vector3.zero)
            flatDir = arCamera.transform.forward;

        Vector3 anchor = arCamera.transform.position + flatDir * probeDistance;

        bool hitPlane = raycaster.Raycast(
            new Ray(anchor + Vector3.up, Vector3.down),
            hits,
            TrackableType.PlaneWithinPolygon);

        if (hitPlane && hits[0].trackable is ARPlane p)
        {
            // accept only large, horizontal planes
            hitPlane = p.alignment == PlaneAlignment.HorizontalUp &&
                       p.size.x >= minPlaneSize && p.size.y >= minPlaneSize;

            if (hitPlane)
            {
                // valid hit: update target
                currentTarget = hits[0].pose.position + Vector3.up * heightOffset;
                arrowPlaced = true;
            }
        }

        // ------------ 2. Show / hide UI & arrow ------------
        if (!arrowPlaced)                             // still searching
        {
            if (arrow.gameObject.activeSelf)
                arrow.gameObject.SetActive(false);

            if (coachingUI && !coachingUI.activeSelf) 
                coachingUI.SetActive(true); // show coaching panel        
            return;
        }

        // first time we place the arrow -> hide coaching panel
        if (!arrow.gameObject.activeSelf)
            arrow.gameObject.SetActive(true);

        if (coachingUI && coachingUI.activeSelf)      
            coachingUI.SetActive(false);   // hide coaching panel

        // ------------ 3. Smooth-move toward (or stay at) target ------------
        float k = 1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f);
        arrow.position = Vector3.Lerp(arrow.position, currentTarget, k);
    }


    // Called when the user taps the screen
    public void changeArrow(Transform arrowswap)
    {
        arrow = arrowswap;
    }

    // ◆◆◆ swap dotted material to transparent after a delay ◆◆◆
    IEnumerator SwapPlanesAfterDelay()
    {
        yield return new WaitForSeconds(swapDelay);

        // A) change every plane already in the scene
        foreach (var plane in planeManager.trackables)
            SetPlaneMaterial(plane, shadowMaterial);

        // B) ensure the prefab (future planes) uses the transparent material
        if (planeManager.planePrefab)
        {
            var mr = planeManager.planePrefab.GetComponentInChildren<MeshRenderer>();
            if (mr) mr.sharedMaterial = shadowMaterial;
        }
    }

    // helper
    void SetPlaneMaterial(ARPlane plane, Material mat)
    {
        var mr = plane.GetComponent<MeshRenderer>();
        if (mr && mr.sharedMaterial != mat)
            mr.sharedMaterial = mat;
    }
    
    public void HideArrow()
    {
        arrowPlaced = false;
        if (arrow) arrow.gameObject.SetActive(false);
        if (coachingUI) coachingUI.SetActive(false);
    
        GPSBearing.CalibrationDone = false;   // <- add this line
    }

}


