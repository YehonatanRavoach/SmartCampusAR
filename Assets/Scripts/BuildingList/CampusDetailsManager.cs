using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
/*
 * CampusDetailsManager.cs
 * -----------------------------------------------------------------------------
 * Manages loading and displaying detailed information for the selected campus,
 * including title, logo, map image, building list, and map points.
 * Data is fetched from Firebase Firestore and displayed in the UI.
 */

public class CampusDetailsManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI campusTitleText;
    public Image campusMapImage;
    public TMP_InputField searchInput;       
    public Image campusLogogImage;

    [Header("Prefabs & Containers")]
    public GameObject buildingPrefab;
    public Transform buildingListContent;
    public GameObject mapPointPrefab;
    public RectTransform mapContainer;
    

    private FirebaseFirestore db;
    private List<BuildingItemGuest> buildingItems = new List<BuildingItemGuest>(); // List to hold all building items

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        
        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchValueChanged);

        LoadCampusData();
    }// Initialize Firestore and set up search input listener

    
    private void OnSearchValueChanged(string searchText)
    {
        string lower = searchText.ToLower();
        foreach (var item in buildingItems)
        {
            bool match = string.IsNullOrEmpty(lower) 
                         || item.BuildingName.ToLower().Contains(lower);
            item.gameObject.SetActive(match);
        }
    }// Filter buildings based on search input

    void LoadCampusData()
    {
        string campusId = CampusDataHolder.selectedCampusId;
        if (string.IsNullOrEmpty(campusId))
        {
            Debug.LogError("❌ campusId is null. Can't load campus data.");
            return;
        }

        
        db.Collection("Campuses")
          .Document(campusId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || !task.Result.Exists)
            {
                Debug.LogError("❌ Failed to load campus document: " + task.Exception);
                return;
            }

            var data = task.Result.ToDictionary();
            campusTitleText.text = data.ContainsKey("name") 
                                   ? data["name"].ToString() 
                                   : "Unknown Campus";
            string logoUrl = data.ContainsKey("logoURL")
                    ? data["logoURL"].ToString()
                    : null;
            if (!string.IsNullOrEmpty(logoUrl))
            {
                StartCoroutine(LoadImage(logoUrl, campusLogogImage));
            }
            else
            {
                Debug.LogWarning("⚠️ logoURL is missing for this campus");
            }
            

            if (data.ContainsKey("mapImageURL") && 
                !string.IsNullOrEmpty(data["mapImageURL"].ToString()))
            {
                StartCoroutine(LoadImage(data["mapImageURL"].ToString(), campusMapImage));
            }
            else
            {
                Debug.LogWarning("⚠️ mapImageURL is missing for this campus");
            }

            
            LoadBuildings(campusId);
        });
    }// Load campus data and initialize UI elements

    void LoadBuildings(string campusId)
    {
        db.Collection("Campuses")
          .Document(campusId)
          .Collection("Buildings")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("❌ Failed to load buildings: " + task.Exception);
                return;
            }

            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                if (doc.Id == "_placeholder") continue; // Ignore placeholder document

                var data      = doc.ToDictionary();
                string name   = data.ContainsKey("buildingName") 
                                ? data["buildingName"].ToString() : "Unnamed";
                string desc   = data.ContainsKey("description")  
                                ? data["description"].ToString()    : "";
                string imageUrl  = data.ContainsKey("imageURL") 
                                ? data["imageURL"].ToString()       : "";
                float  x      = data.ContainsKey("mapX") 
                                ? float.Parse(data["mapX"].ToString()) : 0.5f;
                float  y      = data.ContainsKey("mapY") 
                                ? float.Parse(data["mapY"].ToString()) : 0.5f;

                string coord  = data.TryGetValue("coordinates",  out var c) ? c.ToString() : "Unknown";

                
                GameObject buildingItem = Instantiate(buildingPrefab, buildingListContent);
                var guest = buildingItem.GetComponent<BuildingItemGuest>();
                if (guest != null)
                {
                    guest.Setup(
                        doc.Id,    
                        name,      
                        desc,      
                        imageUrl,
                        coord   
                    );// Initialize the building item with data
                    buildingItems.Add(guest);  
                }
                else
                {
                    Debug.LogError(" buildingPrefab BuildingItemGuest!");
                }

                
                GameObject point = Instantiate(mapPointPrefab, mapContainer);
                var pointText = point.GetComponentInChildren<MapPointNavigation>();
                if (pointText != null)
                {
                    pointText.Setup(
                        doc.Id,    
                        name,      
                        desc      
                           
                    );// Set up the map point with building data
                
                var rt = point.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(x, y);
                rt.anchoredPosition = Vector2.zero;
                }
            }

        });
    }// Load all buildings for the selected campus

    IEnumerator LoadImage(string url, Image img)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("⚠️ Cannot load image: URL is null or empty");
            yield break;
        }

        using (var uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError(" Failed to load image: " + uwr.error);
                yield break;
            }

            var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
            img.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
            img.preserveAspect = false;
        }
    }
}
