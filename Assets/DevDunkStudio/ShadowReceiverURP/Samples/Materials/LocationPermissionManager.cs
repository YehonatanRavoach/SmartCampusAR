using System.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.ProBuilder;



public class LocationPermissionManager : MonoBehaviour
{
    // This variable will hold the location text to display on screen
    
    void Start()
    {
        // When the script starts, request location permission from the user
        RequestLocationPermission();
    }
    
    // Function to request location permission from the user
    void RequestLocationPermission()
    {
#if UNITY_ANDROID

        // Check if the location permission has already been granted by the user
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            // If not granted, request both fine and coarse location permissions
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);

            // Start a coroutine to check the permission status after a short delay
            StartCoroutine(CheckPermissionAfterRequest());
        }
        else
        {
            // If permission is already granted, log a message
            Debug.Log("Location permission has already been granted.");
            StartCoroutine(StartLocationServices());
        }
#elif UNITY_IOS
        // iOS handles location permissions automatically based on the Info.plist configuration.
        // No additional code is needed here for iOS devices.
#endif
    }

    // Coroutine to check if the permission has been granted after the user is prompted
    private IEnumerator CheckPermissionAfterRequest()
    {
        yield return new WaitForSeconds(0.5f);

        if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Debug.Log("Location permission has been granted.");
            StartCoroutine(StartLocationServices());
        }
        else
        {
            Debug.Log("Location permission has been denied.");
            
        }
    }

    private IEnumerator StartLocationServices()
    {
        if (Input.location.isEnabledByUser)
            print("Location services are enabled by the user.");

        Input.location.Start(0.1f,0.1f);

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        if (Input.location.status == LocationServiceStatus.Failed)
        {
            
            yield break;
        }

        
    }


}
