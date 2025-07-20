using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
/******************************************************************************
 * NavigationBuildingLoader.cs
 * -----------------------------------------------------------------------------
 * Fetches the currently‑selected building document from Firestore and:
 *
 *   • Updates UI: building title + hero image (`buildingImage` & `buildingImage1`)
 *   • Parses the “coordinates” string field → latitude / longitude
 *   • Passes the coordinates to <see cref="GPSBearing.SetTarget"/> so the
 *     navigation arrow starts pointing to the building in AR.
 *
 *  Configuration
 *  -------------
 *  • `CampusDataHolder.selectedCampusId` must already hold the campus doc ID.  
 *  • `BuildingDataHolder.docId` must hold the building doc ID.  
 *  • Drag references in the Inspector:
 *        – TextMeshProUGUI for the building name
 *        – Two Image components for portrait/thumbnail
 *        – A <see cref="GPSBearing"/> component in the scene
 *****************************************************************************/
 
public class NavigationBuildingLoader : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI buildingTitleText;
    public Image buildingImage;
    public Image buildingImage1;
    [Header("Logic")]
    public GPSBearing gpsBearing;      
 
    private FirebaseFirestore db;
 
    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
 
        
        if (string.IsNullOrEmpty(CampusDataHolder.selectedCampusId) ||
            string.IsNullOrEmpty(BuildingDataHolder.docId))
        {
            Debug.LogError("No building selected – returning to campus screen");
            
            return;
        }
 
        FetchBuilding();
    }
 
    void FetchBuilding() // Fetches the building document from Firestore
    {
        db.Collection("Campuses")
          .Document(CampusDataHolder.selectedCampusId)
          .Collection("Buildings")
          .Document(BuildingDataHolder.docId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("Failed to fetch building doc");
                return;
            }
 
            var data = task.Result.ToDictionary();
 
            
            string name = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "Building";
            buildingTitleText.text = name;

            
            if (data.ContainsKey("imageURL"))
            {
                string imgUrl = data["imageURL"].ToString();
                StartCoroutine(LoadImage(imgUrl, buildingImage));
                StartCoroutine(LoadImage(imgUrl, buildingImage1));
            }
 
            
            double lat = 0;
            double lon = 0;
            if (data.ContainsKey("coordinates"))
            {
                ParseCoords(data["coordinates"].ToString(), out lat, out lon);
            }

            
            gpsBearing.SetTarget(lat, lon);
            
            Debug.Log("Extracted Coords:" + lat);
            Debug.Log("Extracted Coords:" + lon);
        });
    }
 
    bool ParseCoords(string raw, out double lat, out double lon) // Parses a "lat, lon" string into latitude and longitude
    {
        lat = lon = 0;
        var parts = raw.Split(',');
        if (parts.Length != 2) return false;
 
        string[] p1 = parts[0].Trim().Split(' '); // lat + N/S
        string[] p2 = parts[1].Trim().Split(' '); // lon + E/W
 
        double.TryParse(p1[0], out lat);
        double.TryParse(p2[0], out lon);
 
        if (p1.Length > 1 && p1[1].Equals("S", System.StringComparison.OrdinalIgnoreCase))// Check for N/S and E/W indicators
            lat *= -1; 
        if (p2.Length > 1 && p2[1].Equals("W", System.StringComparison.OrdinalIgnoreCase))// Check for N/S and E/W indicators
            lon *= -1;
 
        return true;
    }
 
    System.Collections.IEnumerator LoadImage(string url, Image img)
    {
        using (var uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
                img.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), Vector2.one*0.5f);
            }
        }
    }
}
