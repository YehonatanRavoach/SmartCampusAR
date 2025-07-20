using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Firebase.Extensions;
using UnityEngine;

/// <summary>
/// Fetches names of all campuses with status "active" from Firestore.
/// Used to provide campus selection options in UI screens (e.g., Request to Manage).
/// </summary>
public static class CampusFetcher
{
    public static Task<List<string>> GetCampusNames()
    {
        var db = FirebaseFirestore.DefaultInstance;
        var campusesRef = db.Collection("Campuses").WhereEqualTo("status", "active");

        var tcs = new TaskCompletionSource<List<string>>();
        List<string> names = new List<string>();

        campusesRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to fetch campus names");
                tcs.SetResult(new List<string>());
                return;
            }

            foreach (var doc in task.Result.Documents)
            {
                if (doc.TryGetValue("name", out string campusName))
                {
                    names.Add(campusName);
                    Debug.Log($"Found campus: {campusName}");
                }
            }

            tcs.SetResult(names);
        });

        return tcs.Task;
    }
}

/*
    --- Key Concepts ---

    - Fetches active campuses only: Queries Firestore for campuses with status == "active".
    - Asynchronous: Uses Task pattern for non-blocking UI updates.
    - Error Handling: Returns an empty list and logs error if Firestore query fails.
    - Integration: Used by UI scripts to populate dropdowns for campus selection.

    --- Example Usage ---

    var campusNames = await CampusFetcher.GetCampusNames();
*/