using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;     // For IPointerClickHandler
using Firebase;
using Firebase.Firestore;
using Firebase.Storage;
using System.Collections;
using System.Collections.Generic;
using System;
using Firebase.Extensions;
/******************************************************************************
 * MapMarkerManager.cs
 * -----------------------------------------------------------------------------
 * Handles a “tap-to-place” building workflow on a 2D campus map:
 *
 *   • User taps the map → converts screen-point to *normalized* (0-1) coords
 *     and drops / moves a pin (`markerPrefab`) inside `mapRect`.
 *   • Optional image picker (NativeGallery) → shows thumbnail, uploads PNG to
 *     Firebase Storage, and stores the download-URL in Firestore.
 *   • Saves a new building document containing:
 *        buildingName, description, mapX, mapY, imageURL, createdAt
 *

 *****************************************************************************/

public class MapMarkerManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Firebase")]
    private FirebaseFirestore db;
    private StorageReference storageRef;

    [Header("UI References")]
    public RectTransform mapRect;           // The RectTransform of your campus map Image
    public TMP_InputField buildingNameInput;
    public TMP_InputField buildingDescInput;
    public Button saveButton;
    public Button selectImageButton;        // Button to select image from gallery or camera
    public Image previewImage;              // Show selected/taken image

    [Header("Marker")]
    public GameObject markerPrefab;         // A small UI prefab (e.g., a pin icon) to mark the chosen point
    private GameObject currentMarker;       // The currently placed marker

    // Map coordinates normalized in [0,1]
    private float mapX = -1f;
    private float mapY = -1f;

    // Selected image info
    private Texture2D selectedTexture;
    private string imagePath = "";

    void Start()
    {
        // Initialize Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
                storageRef = FirebaseStorage.DefaultInstance.RootReference;
                Debug.Log("Firebase Firestore and Storage are ready.");

                // Wire up button events
                if (saveButton != null)
                    saveButton.onClick.AddListener(OnSaveClicked);
                if (selectImageButton != null)
                    selectImageButton.onClick.AddListener(OnSelectImageClicked);
            }
            else
            {
                Debug.LogError("Firebase dependencies not resolved: " + task.Result);
            }
        });
    }

    /// <summary>
    /// Called when the user clicks/taps on the map image.
    /// Converts the screen point to local normalized coordinates and places a marker.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // Convert local coordinates to normalized coordinates [0,1]
            Vector2 normalized = LocalToNormalized(localPoint);
            mapX = normalized.x;
            mapY = normalized.y;

            Debug.Log($"Map clicked: local = {localPoint}, normalized = ({mapX:F3}, {mapY:F3})");

            PlaceMarker(normalized);
        }
    }

    /// <summary>
    /// Convert local position (assuming pivot at center) to normalized coordinates.
    /// Adjust this if your pivot is different.
    /// </summary>
    private Vector2 LocalToNormalized(Vector2 localPoint)
    {
        float width = mapRect.rect.width;
        float height = mapRect.rect.height;
        float nx = (localPoint.x / width) + 0.5f;
        float ny = (localPoint.y / height) + 0.5f;
        return new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
    }

    /// <summary>
    /// Place or move the marker on the map based on normalized coordinates.
    /// </summary>
    private void PlaceMarker(Vector2 normalized)
    {
        if (markerPrefab == null) return;

        if (currentMarker == null)
        {
            currentMarker = Instantiate(markerPrefab, mapRect);
        }

        RectTransform markerRect = currentMarker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            float localX = (normalized.x - 0.5f) * mapRect.rect.width;
            float localY = (normalized.y - 0.5f) * mapRect.rect.height;
            markerRect.anchoredPosition = new Vector2(localX, localY);
        }
    }

    /// <summary>
    /// Called when the user clicks the "Select Image" button.
    /// This example uses NativeGallery; adjust as needed.
    /// </summary>
    private void OnSelectImageClicked()
{
    NativeGallery.GetImageFromGallery((path) =>
    {
        if (!string.IsNullOrEmpty(path))
        {
            LoadAndDisplayImage(path);
        }
        else
        {
            Debug.LogWarning("No image selected or permission denied.");
        }
    }, "Select Image from Gallery", "image/*");
}

    /// <summary>
    /// Loads an image from disk and shows it in the preview.
    /// </summary>
    private void LoadAndDisplayImage(string path)
    {
        Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024, false, false);
        if (tex != null)
        {
            selectedTexture = tex;
            imagePath = path;
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            if (previewImage != null)
                previewImage.sprite = sp;
        }
    }

    /// <summary>
    /// Called when the user clicks the "Save" button.
    /// Uploads the image (if available) and saves the building data (name, description, map coords, image URL) to Firestore.
    /// </summary>
    private void OnSaveClicked()
    {
        string buildingName = buildingNameInput.text;
        string buildingDesc = buildingDescInput.text;

        if (string.IsNullOrEmpty(buildingName))
        {
            Debug.LogWarning("Please enter a building name.");
            return;
        }
        if (mapX < 0f || mapY < 0f)
        {
            Debug.LogWarning("Please click on the map to mark a location.");
            return;
        }

        // If an image was selected, upload it first, then save the data.
        if (selectedTexture != null)
        {
            StartCoroutine(UploadImageToFirebaseStorage(selectedTexture, (downloadUrl) =>
            {
                SaveBuildingData(buildingName, buildingDesc, mapX, mapY, downloadUrl);
            }));
        }
        else
        {
            // No image selected; just save building data with empty image URL.
            SaveBuildingData(buildingName, buildingDesc, mapX, mapY, "");
        }
    }

    /// <summary>
    /// Saves the building data to Firestore.
    /// </summary>
    private void SaveBuildingData(string name, string desc, float mapX, float mapY, string imageUrl)
    {
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            { "buildingName", name },
            { "description", desc },
            { "mapX", mapX },
            { "mapY", mapY },
            { "imageURL", imageUrl },
            { "createdAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
        };

        DocumentReference docRef = db.Collection("Buildings").Document();
        docRef.SetAsync(data).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error saving building: " + task.Exception);
            }
            else
            {
                Debug.Log($"Building saved successfully with doc ID: {docRef.Id}");
                // Optionally clear the form, marker, and preview image here.
            }
        });
    }

    /// <summary>
    /// Coroutine that uploads the selected image to Firebase Storage and returns the download URL via callback.
    /// </summary>
    IEnumerator UploadImageToFirebaseStorage(Texture2D texture, Action<string> onComplete)
    {
        string fileName = "Building_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        StorageReference imagesRef = storageRef.Child("buildings_images").Child(fileName);

        byte[] imgData = texture.EncodeToPNG();
        var uploadTask = imagesRef.PutBytesAsync(imgData);
        yield return new WaitUntil(() => uploadTask.IsCompleted);

        if (uploadTask.Exception != null)
        {
            Debug.LogError("Error uploading image: " + uploadTask.Exception);
            onComplete("");
            yield break;
        }

        var getUrlTask = imagesRef.GetDownloadUrlAsync();
        yield return new WaitUntil(() => getUrlTask.IsCompleted);
        if (getUrlTask.Exception != null)
        {
            Debug.LogError("Error getting download URL: " + getUrlTask.Exception);
            onComplete("");
        }
        else
        {
            string downloadUrl = getUrlTask.Result.ToString();
            Debug.Log("Image uploaded. Download URL: " + downloadUrl);
            onComplete(downloadUrl);
        }
    }
}
