using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Firestore;
using Firebase;
using Firebase.Extensions;
using System.Collections.Generic;

/// <summary>
/// Responsible only for loading, displaying and filtering the campus list UI.
/// All update/delete actions are handled exclusively in CampusItem.cs.
/// </summary>
public class CampusListManager : MonoBehaviour
{
    [Header("Search")]
    public TMP_InputField searchInput;

    public GameObject campusPrefab;
    public Transform contentParent;

    private FirebaseFirestore db;
    private List<CampusData> campuses = new List<CampusData>();

    private static readonly Dictionary<string, int> STATUS_ORDER = new()
    {
        { "pending", 0 },
        { "active",  1 },
        { "reject",  2 }
    };

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;

                if (searchInput != null)
                {
                    searchInput.onValueChanged.AddListener(OnSearchValueChanged);
                    OnSearchValueChanged(""); // Load UI even if no search yet
                }

                LoadCampuses();
            }
        });
    }

    /// <summary>
    /// Loads all campuses from Firestore and updates the UI.
    /// </summary>
    public void LoadCampuses()
    {
        db.Collection("Campuses").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                campuses.Clear();

                foreach (var doc in task.Result.Documents)
                {
                    var d = doc.ToDictionary();
                    campuses.Add(new CampusData
                    {
                        docId = doc.Id,
                        name = d.GetValueOrDefault("name", "").ToString(),
                        city = d.GetValueOrDefault("city", "").ToString(),
                        country = d.GetValueOrDefault("country", "").ToString(),
                        description = d.GetValueOrDefault("description", "").ToString(),
                        logoURL = d.GetValueOrDefault("logoURL", "").ToString(),
                        status = d.GetValueOrDefault("status", "pending").ToString().ToLower()
                    });
                }

                // Sort by status order: pending → active → reject
                campuses.Sort((a, b) =>
                    STATUS_ORDER.GetValueOrDefault(a.status, 3)
                       .CompareTo(
                    STATUS_ORDER.GetValueOrDefault(b.status, 3)));

                RefreshUI(campuses);
            }
            else
            {
                Debug.LogError("Failed to load campuses: " + task.Exception);
            }
        });
    }

    /// <summary>
    /// Updates the UI with a given campus list.
    /// </summary>
    void RefreshUI(List<CampusData> listToShow)
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var campus in listToShow)
        {
            GameObject item = Instantiate(campusPrefab, contentParent);
            CampusItem campusItem = item.GetComponentInChildren<CampusItem>();
            if (campusItem != null)
            {
                campusItem.Setup(this, campus);
            }
        }
    }

    /// <summary>
    /// Filters the campus list by user input.
    /// </summary>
    public void OnSearchValueChanged(string userInput)
    {
        string lower = userInput.ToLower();

        var filtered = campuses.FindAll(c =>
            c.name.ToLower().Contains(lower) ||
            c.city.ToLower().Contains(lower));

        // Sort filtered list as well
        filtered.Sort((a, b) =>
            STATUS_ORDER.GetValueOrDefault(a.status, 3)
               .CompareTo(
            STATUS_ORDER.GetValueOrDefault(b.status, 3)));

        RefreshUI(filtered);
    }
}

/// <summary>
/// Local data structure for campus UI.
/// </summary>
[System.Serializable]
public class CampusData
{
    public string docId;
    public string name;
    public string city;
    public string country;
    public string description;
    public string logoURL;
    public string status;
}
