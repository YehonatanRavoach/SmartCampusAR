using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// UI controller for one administrator row.  Responsible for:
/// • Setting up text, images, and event listeners.<br/>
/// • Updating admin status via <c>setAdminStatus</c> Cloud Function.<br/>
/// • Deleting admin profile via <c>deleteAdmin</c> Cloud Function.<br/>
/// • Ensuring dropdown state matches server state only after success.
/// </summary>
public class AdminListManager : MonoBehaviour
{
    [Header("Prefab + Content")]
    public GameObject adminPrefab;
    public Transform contentParent;

    [Header("Search")]
    public TMP_InputField searchInput;

    private FirebaseFirestore db;
    private List<AdminDataLocal> allAdmins = new List<AdminDataLocal>();
    public GameObject loadingSpinner;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;

                if (searchInput != null)
                    searchInput.onValueChanged.AddListener(OnSearchValueChanged);

                LoadAllAdminsFromFirestore();
            }
            else
            {
                Debug.LogError("Firebase dependencies not resolved: " + task.Result);
            }
        });
    }

    /// <summary>
    /// Loads all admin profiles from Firestore and updates the UI.
    /// </summary>
    public void LoadAllAdminsFromFirestore()
    {
        db.Collection("Admin_Profiles")
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(snapshotTask =>
          {
              if (snapshotTask.IsFaulted)
              {
                  Debug.LogError("Failed to load admin data: " + snapshotTask.Exception);
                  return;
              }

              allAdmins.Clear();
              foreach (var doc in snapshotTask.Result.Documents)
              {
                  var d = doc.ToDictionary();
                  allAdmins.Add(new AdminDataLocal
                  {
                      docId = doc.Id,
                      adminName = d.GetValueOrDefault("adminName", "").ToString(),
                      email = d.GetValueOrDefault("email", "").ToString(),
                      campusId = d.GetValueOrDefault("campusId", "").ToString(),
                      role = d.GetValueOrDefault("role", "").ToString(),
                      status = d.GetValueOrDefault("status", "").ToString().ToLower(),
                      employeeApprovalFileURL = d.GetValueOrDefault("employeeApprovalFileURL", "").ToString(),
                      adminPhotoURL = d.GetValueOrDefault("adminPhotoURL", "").ToString()
                  });
              }

              // Status priority: pending → active → reject
              Dictionary<string, int> order = new()
            {
                { "pending", 0 },
                { "active",  1 },
                { "reject",  2 }
            };

              allAdmins.Sort((a, b) =>
                  order.GetValueOrDefault(a.status, 3)
                    .CompareTo(
                  order.GetValueOrDefault(b.status, 3)));

              RefreshUI(allAdmins);
          });
    }

    /// <summary>
    /// Rebuilds the admin list UI.
    /// </summary>
    public void RefreshUI(List<AdminDataLocal> adminsToShow)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var admin in adminsToShow)
        {
            var itemGO = Instantiate(adminPrefab, contentParent);
            var item = itemGO.GetComponentInChildren<AdminItem>();
            if (item != null)
            {
                item.Setup(this, admin);
            }
            else
                Debug.LogError("No AdminItem component found on prefab!");
        }
    }

    /// <summary>
    /// Filters the admin list by user input.
    /// </summary>
    private void OnSearchValueChanged(string userInput)
    {
        string lower = userInput.ToLower();
        var filtered = allAdmins.FindAll(a =>
            a.adminName.ToLower().Contains(lower) ||
            a.email.ToLower().Contains(lower)
        );
        RefreshUI(filtered);
    }
}