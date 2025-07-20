    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using System.Collections.Generic;
    using Firebase;
    using Firebase.Firestore;
    using Firebase.Extensions;
    /******************************************************************************
    * CampusesList.cs
    * -----------------------------------------------------------------------------
    * Populates a vertical UI list with all **active** campus documents from
    * Firestore and supports live filtering via a search bar.
    *
    *  Workflow
    *  --------
    *  • On Start() the script:
    *        1. Checks Firebase dependencies.
    *        2. Queries the *Campuses* collection where `status == "active"`.
    *        3. Instantiates `campusPrefab` under `contentParent` for each doc.
    *        4. Passes the campus name, document‑ID, and logo‑URL into
    *           <see cref="CampusStartItem.Setup"/> so the prefab can handle
    *           its own thumbnail loading and click navigation.
    *  • `searchInput` uses <see cref="FilterList(String)"/> to show/hide items
    *    whose name contains the user’s search substring (case‑insensitive).
    *
    *  Inspector references
    *  --------------------
    *      campusPrefab      – Prefab containing a <c>CampusStartItem</c>.
    *      contentParent     – Scroll‑view content transform.
    *      searchInput       – TMP_InputField used for filtering.
    *****************************************************************************/

    public class CampusesList : MonoBehaviour
    {
        [Header("UI")]
        public GameObject campusPrefab;
        public Transform contentParent;
        public TMP_InputField searchInput;

        private FirebaseFirestore db;
        private List<GameObject> campusItems = new List<GameObject>();

        void Start()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result == DependencyStatus.Available)
                {
                    db = FirebaseFirestore.DefaultInstance;
                    LoadCampuses();
                }
                else
                {
                    Debug.LogError("Firebase not ready: " + task.Result);
                }
            });

            searchInput.onValueChanged.AddListener(FilterList);
        }

        void LoadCampuses()
        {
            db.Collection("Campuses").WhereEqualTo("status","active").GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to load campuses: " + task.Exception);
                    return;
                }

                QuerySnapshot snapshot = task.Result;

                foreach (DocumentSnapshot doc in snapshot.Documents)
                {
                    Dictionary<string, object> data = doc.ToDictionary();
                    
                    if (!data.ContainsKey("name")) continue;
                
                    string campusName = data["name"].ToString();
                    string logoUrl = data.ContainsKey("logoURL")
                        ? data["logoURL"].ToString()
                        : null;
                    GameObject go = Instantiate(campusPrefab, contentParent);
                
                    // 1) Grab the CampusItem component
                    CampusStartItem item = go.GetComponentInChildren<CampusStartItem>();
                    TextMeshProUGUI textComponent = go.GetComponentInChildren<TextMeshProUGUI>();
                        if (textComponent != null)
                            textComponent.text = campusName;
                    if (item != null)
                    {
                        // 2) Pass both name AND doc.Id into it
                        
                        item.Setup(campusName, doc.Id,logoUrl);
                    }
                
                    campusItems.Add(go);
                }
            });
        }

        void FilterList(string searchText)// Filters the campus list based on the search input
        {
            searchText = searchText.ToLower();

            foreach (GameObject item in campusItems)
            {
                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
                bool shouldShow = text != null && text.text.ToLower().Contains(searchText);
                item.SetActive(shouldShow);
            }
        }
    }
