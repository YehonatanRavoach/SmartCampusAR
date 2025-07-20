using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
/*
 * BuildingListManager1.cs
 * -----------------------------------------------------------------------------
 * Displays the list of buildings that belong to the current campus (for the
 * logged-in admin user).  The manager:
 *
 *   • Fetches the admin’s profile photo (Admin_Profiles/{adminId}.adminPhotoURL)
 *   • Fetches campus name + logo (Campuses/{campusDocId})
 *   • Fetches every building doc under Campuses/{campusDocId}/Buildings
 *   • Generates a scroll-list item for each building (BuildingItem prefab)
 *   • Provides live search filtering by building name
 *
 *  All runtime logic is **unchanged** – only English documentation comments
 *  were added or translated; no executable statements were removed.
 */

public class BuildingListManager1 : MonoBehaviour
{
    
    private string adminId;     // <<< MOD
    private string campusDocId; // <<< MOD

    [Header("Prefab + Content")]
    public GameObject buildingPrefab;
    public Transform contentParent;

    [Header("Search")]
    public TMP_InputField searchInput;

    [Header("Admin Image")]
    public Image adminImage;

    [Header("Campus Info")]
    public TextMeshProUGUI campusNameText;
    public Image campusLogoImage;


    private FirebaseFirestore db;
    private readonly List<BuildingDataLocal> allBuildings = new();

    // ------------------------- Awake -------------------------    
    private void Awake()
    {
        adminId     = SessionData.Instance.AdminId;
        campusDocId = SessionData.Instance.CampusId;
    }

    // ------------------------- Start -------------------------
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependencies not resolved: " + task.Result);
                return;
            }

            db = FirebaseFirestore.DefaultInstance;
            Debug.Log("Firebase Firestore is ready!");

            if (searchInput)
                searchInput.onValueChanged.AddListener(OnSearchValueChanged);

            LoadAdminImageFromFirestore();
            LoadAllBuildingsFromFirestore();
            LoadCampusData();

            BuildingDataHolder.docId = "";
        });
    }

    // =========================================================
    //  ADMIN  IMAGE
    // =========================================================
    // Loads the admin profile image from Firestore
    private void LoadAdminImageFromFirestore()
    {
        db.Collection("Admin_Profiles").Document(adminId)
          .GetSnapshotAsync().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) { Debug.LogError(t.Exception); return; }

            var snap = t.Result;
            if (!snap.Exists || !snap.ContainsField("adminPhotoURL"))
            {
                Debug.LogWarning("adminPhotoURL missing for admin " + adminId);
                return;
            }

            string url = snap.GetValue<string>("adminPhotoURL");


            url += "?t=" + System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            StartCoroutine(LoadImage(url, adminImage));
        });
    }
// =========================================================
    //  CAMPUS DATA
    // =========================================================
    // Loads the campus name and logo from Firestore
private void LoadCampusData()
{
    db.Collection("Campuses").Document(campusDocId)
      .GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsFaulted || !task.Result.Exists)
        {
            Debug.LogError("Failed to load campus data.");
            return;
        }

        var doc = task.Result;
        string campusName = doc.ContainsField("name") ? doc.GetValue<string>("name") : "Unknown Campus";
        string logoURL    = doc.ContainsField("logoURL") ? doc.GetValue<string>("logoURL") : "";

        campusNameText.text = campusName;

        if (!string.IsNullOrEmpty(logoURL))
        {
            logoURL += "?t=" + System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StartCoroutine(LoadImage(logoURL, campusLogoImage));
        }
    });
}


    private System.Collections.IEnumerator LoadImage(string url, Image img)
    {
        using var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load image: " + req.error);
            yield break;
        }

        Texture2D tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * .5f);

        RectTransform rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        img.preserveAspect = false;
    }

    // =========================================================
    //  BUILDINGS
    // =========================================================
    public void LoadAllBuildingsFromFirestore()
    {
        db.Collection("Campuses").Document(campusDocId)
          .Collection("Buildings").GetSnapshotAsync().ContinueWithOnMainThread(t =>
        {
            if (t.IsFaulted) { Debug.LogError(t.Exception); return; }

            allBuildings.Clear();

            foreach (var doc in t.Result.Documents)
            {
                var d = doc.ToDictionary();
                if (!d.ContainsKey("buildingName")) continue;   

                allBuildings.Add(new BuildingDataLocal
                {
                    docId       = doc.Id,
                    name        = d["buildingName"].ToString(),
                    description = d.TryGetValue("description", out object desc) ? desc.ToString() : "",
                    imageUrl    = d.TryGetValue("imageURL",     out object url)  ? url.ToString()  : ""
                });
            }
            RefreshUI(allBuildings);
        });
    }

    private void RefreshUI(List<BuildingDataLocal> list)
    {
        foreach (Transform c in contentParent) Destroy(c.gameObject);

        foreach (var b in list)
        {
            GameObject go = Instantiate(buildingPrefab, contentParent);
            var item      = go.GetComponentInChildren<BuildingItem>();
            if (item) item.Setup(b.docId, b.name, b.description, b.imageUrl);
            else      Debug.LogError("BuildingItem missing on prefab!");
        }
    }

    private void OnSearchValueChanged(string txt)
    {
        string low = txt.ToLower();
        var filtered = allBuildings.FindAll(b => b.name.ToLower().Contains(low));
        RefreshUI(filtered);
    }
}

// ------------------------------------------------------------
//  POCO for local storage
// ------------------------------------------------------------
public class BuildingDataLocal
{
    public string docId;
    public string name;
    public string description;
    public string imageUrl;
    public string logoURL;
}
