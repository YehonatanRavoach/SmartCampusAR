using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.XR.ARFoundation;

/******************************************************************************
 * PermissionManager.cs
 * -----------------------------------------------------------------------------
 * Centralised Android‑runtime permission flow for an AR‑navigation scene.
 *
 *  1. Requests the CAMERA, FINE LOCATION, and COARSE LOCATION permissions in
 *     sequence (skips each if already granted).  
 *  2. Once all permissions have been addressed—granted or denied—enables the
 *     AR Session GameObject (`arsession`).  
 *  3. Starts Unity’s <c>Input.location</c> service and waits for it to
 *     initialise (20‑second timeout).  
 *
 *  UI feedback
 *  -----------
 *  • `locationText` (public string) is updated with simple status messages
 *    that you can show in a debug HUD or the Inspector.  
 *  • If any permission is denied the script still proceeds, but location
 *    may fail to initialise.  
 *****************************************************************************/

public class PermissionManager : MonoBehaviour
{

    [Header("Debug UI (optional)")]
    public string locationText = "Waiting for permissions...";
    public GameObject arsession;

    void Start()
    {
        // Start the overall init flow
        StartCoroutine(InitializePermissionsAndServices());
    }

    IEnumerator InitializePermissionsAndServices() // Coroutine to handle permissions and location services
    {
        // --- 1) CAMERA PERMISSION ---
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera)) // Check if the Camera permission is already granted
        {
            bool done = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => done = true; // Handle the case where the user grants permission
            callbacks.PermissionDenied += _ => done = true; // Handle the case where the user denies permission
            callbacks.PermissionDeniedAndDontAskAgain += _ => done = true; // Handle the case where the user denies permission and selects "Don't ask again"

            Permission.RequestUserPermission(Permission.Camera, callbacks);
            // wait for user to respond
            yield return new WaitUntil(() => done);
        }

        // --- 2) FINE LOCATION PERMISSION ---
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)) // Check if the Fine Location permission is already granted
        {
            bool done = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => done = true;
            callbacks.PermissionDenied += _ => done = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => done = true;

            Permission.RequestUserPermission(Permission.FineLocation, callbacks);
            yield return new WaitUntil(() => done);
        }

        // --- 3) COARSE LOCATION PERMISSION ---
        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation)) // Check if the Coarse Location permission is already granted
        {
            bool done = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += _ => done = true;
            callbacks.PermissionDenied += _ => done = true;
            callbacks.PermissionDeniedAndDontAskAgain += _ => done = true;

            Permission.RequestUserPermission(Permission.CoarseLocation, callbacks);
            yield return new WaitUntil(() => done);
        }

        arsession.SetActive(true);

        // --- 4) START LOCATION SERVICES ---
        yield return StartCoroutine(StartLocationServices());
    }

    private IEnumerator StartLocationServices() // Coroutine to start Unity's location services
    {
        if (!Input.location.isEnabledByUser)
        {
            locationText = "Location services disabled.";
            yield break;
        }

        Input.location.Start(0.1f, 0.1f);
        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait-- > 0)
        {
            locationText = $"Initializing location... ({maxWait}s)";
            yield return new WaitForSeconds(1f);
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            locationText = "Failed to start location.";
            yield break;
        }

        locationText = "Location services active.";
        yield break;
    }


}
