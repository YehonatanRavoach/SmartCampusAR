using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Enables the MeshRenderer on exactly one plane: the lowest plane
/// whose X*Y size exceeds <see cref="minPlaneSize"/>.
/// All other planes keep tracking but their visuals (and shadows) are hidden,
/// so you get a single, clean shadow under your arrow.
/// </summary>
[RequireComponent(typeof(ARPlaneManager))]
public class KeepLowestLargePlane : MonoBehaviour
{
    [Tooltip("Minimum side length (in metres) a plane must have to be "
             + "considered the floor.")]
    [SerializeField] float minPlaneSize = 1.0f;   // 1 m × 1 m

    ARPlaneManager planeManager;
    ARPlane floorPlane;           // the one renderer we keep enabled
    [SerializeField] Material shadowMat;   // drag TransparentWithShadows

    void Awake()
    {
        planeManager = GetComponent<ARPlaneManager>();
        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnDestroy()
    {
        if (planeManager != null)
            planeManager.planesChanged -= OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        // Re-evaluate floor candidate when planes are added or updated
        foreach (var p in args.added)
            EvaluateCandidate(p);

        foreach (var p in args.updated)
            EvaluateCandidate(p);

        // Enable renderer only on the chosen floor plane
        foreach (var plane in planeManager.trackables)
        {
            var mr = plane.GetComponent<MeshRenderer>();
            if (!mr) continue;

            mr.enabled = (plane == floorPlane);
        }
        if (floorPlane)                       // make sure renderer exists
        {
            var mr = floorPlane.GetComponent<MeshRenderer>();
            if (mr && mr.sharedMaterial != shadowMat)
                mr.sharedMaterial = shadowMat;         // shadowMat = TransparentWithShadows
        }
    }

    void EvaluateCandidate(ARPlane p)
    {
        // Consider only horizontal upward-facing planes
        if (p.alignment != PlaneAlignment.HorizontalUp)
            return;

        // Consider only planes larger than the size threshold
        if (p.size.x < minPlaneSize || p.size.y < minPlaneSize)
            return;

        // Pick lowest plane (smallest world-space Y)
        if (floorPlane == null ||
            p.transform.position.y < floorPlane.transform.position.y)
        {
            floorPlane = p;
        }
    }
}
