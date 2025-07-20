using UnityEngine;
using UnityEngine.EventSystems;
using Firebase.Firestore;
using Firebase.Extensions;
/*
 * MapPointItem.cs
 * -----------------------------------------------------------------------------
 * UI component placed on the campus map (inside the map panel) that stores both
 * the campus document ID and the building document ID as well as the raw
 * coordinate string.  
 *  
 * • `Setup()` must be called immediately after the prefab is instantiated in
 *   order to provide the Firestore path information.  
 * • `OnPointerClick()` simply prints the coordinates for debugging, but you can
 *   extend it to open a detailed info panel or anything else you need.  
 */

public class MapPointItem : MonoBehaviour
{
    
    private string campusDocId;
    private string buildingId;
    private string rawCoordinates; 

    public void Setup(string campusId, string id, string coords)
    {
        campusDocId      = campusId;
        buildingId       = id;
        
        rawCoordinates   = coords;
    }// Set up the map point with campus and building data
    

    public void OnPointerClick()
    {
      
      Debug.Log("Coordinates: " + rawCoordinates);
        
        // You can extend this method to show a detailed info panel or perform
        // other actions based on the clicked map point.
        // For example, you could open a new scene with more details about the
        // building or campus, or display a tooltip with additional information.
    }// Handle pointer click event to show coordinates or perform other actions
}
