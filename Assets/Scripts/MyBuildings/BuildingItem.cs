using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Firebase.Firestore;
using Firebase.Extensions;
/**************************************************************************
 * BuildingItem.cs
 * ------------------------------------------------------------------------
 * UI component used by *BuildingListManager1* to represent a single building
 * row.  Responsibilities:
 *
 *   • Displays name, description, and optional thumbnail image.  
 *   • Handles **Edit** → stores docId in <see cref="BuildingDataHolder"/>
 *     and loads the “EditBuildingpage1” scene.  
 *   • Handles **Delete** → shows a confirmation dialog, then deletes the
 *     Firestore document:
 *       Campuses/{campusDocId}/Buildings/{docId}
 *     and refreshes the list when successful.  
 *

 **************************************************************************/

 
public class BuildingItem : MonoBehaviour
{

    private string campusDocId; // Get the campus ID from the static holder
    public string BuildingName { get; private set; } // Property to expose the building name

    [Header("UI References")]
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingDescriptionText;
    public Image buildingImage;  // If you display an image
    public Button editButton;
    public Button deleteButton;
    
    [Header("Confirmation Dialog")]
    public GameObject confirmDeletePrefab; 
 
    private string docId;
    
    void Awake()
    {
         campusDocId = SessionData.Instance.CampusId;
    } // Private field to store Firestore document ID
 
    /// <summary>
    /// Setup is called right after instantiating this prefab from BuildingListManager.
    /// </summary>
    public void Setup(string documentId, string name, string desc, string imageUrl)
    {
        docId = documentId;  // Save the doc ID locally.
        // Populate text fields
        buildingNameText.text = name;
        buildingDescriptionText.text = desc;
 
        //(Optional) Load the image from 'imageUrl' if you wish:
        StartCoroutine(LoadImage(imageUrl, buildingImage));
 
        // Wire up the edit button
        editButton.onClick.RemoveAllListeners();
        editButton.onClick.AddListener(OnEditClicked);

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeletePrefab);
        }

        
    }


    private void OnDeletePrefab()
    {
        // Show the confirmation dialog
        if (confirmDeletePrefab != null)
        {
            // Typically, instantiate as a child of the top-level Canvas
            // or current UI root (transform.root might be your top-level).
            GameObject dialogObj = Instantiate(confirmDeletePrefab, transform.root);
            ConfirmationDialog dialog = dialogObj.GetComponentInChildren<ConfirmationDialog>();
            L10n.Get("confirm_delete_building", localizedText =>
            {
                dialog.Initialize(
                    localizedText,
                    OnDeleteClicked,
                    () => Debug.Log("Building delete cancelled")
                );
            });


        }
        else
        {
            Debug.LogError("confirmDeletePrefab not assigned in BuildingItem.");
        }
    }


    private void OnDeleteClicked()
{
    Debug.Log($"🗑️ Deleting docId='{docId}', campusDocId='{campusDocId}'");

    if (string.IsNullOrEmpty(docId) || string.IsNullOrEmpty(campusDocId))
    {
        Debug.LogError("🚫 Cannot delete: docId or campusDocId is null or empty");
        return;
    }

    FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
    DocumentReference docRef = db.Collection("Campuses")
                                 .Document(campusDocId)
                                 .Collection("Buildings")
                                 .Document(docId);

    docRef.DeleteAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Error deleting document: " + task.Exception);
        }
        else
        {
            Debug.Log("✅ Building deleted successfully!");
            Destroy(this.gameObject);
            FindObjectOfType<BuildingListManager1>()?.LoadAllBuildingsFromFirestore();
        }
    });
}

    private void OnEditClicked()
    {
        // Show in console what we have
        Debug.Log($"OnEditClicked: docId = {docId}");
 
        // Put this doc ID in the static holder
        BuildingDataHolder.docId = docId;
 
        // Load the edit scene
        SceneManager.LoadScene("EditBuildingpage1");
    }
    System.Collections.IEnumerator LoadImage(string url, Image image)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();
 
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Get the downloaded texture
                Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;
 
                // Create a sprite from the texture
                image.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
 
                // Adjust the image RectTransform to fill the parent space (customize as needed)
                RectTransform rectTransform = image.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;   // (0,0)
                rectTransform.anchorMax = Vector2.one;    // (1,1)
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                image.preserveAspect = false;
            }
            else
            {
                Debug.LogError("Failed to load image: " + request.error);
            }
        }
    }
}