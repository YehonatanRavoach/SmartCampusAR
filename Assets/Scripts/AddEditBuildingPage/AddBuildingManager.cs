using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;
using Firebase;
using Firebase.Firestore;
using Firebase.Storage;
using System.Threading.Tasks;
using UnityEngine.Android;
using Firebase.Extensions;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
/*
 * AddBuildingManager.cs
 * -----------------------------------------------------------------------------
 * Handles the “Add / Edit Building” flow:
 * 1. Panel 1 – basic form fields (name, description, GPS).
 * 2. Panel 2 – campus map, marker placement, and final Save.
 * Performs image capture / gallery pick, uploads to Firebase Storage,
 * writes the building document to Firestore, and places markers on the map.
 */


public class AddBuildingManager : MonoBehaviour
{


    private string campusDocId;
    private string adminid;
    



    [Header("Panel References")]
    
    public GameObject panel1;
    public GameObject panel2;

    [Header("Required Fields in Panel1")]
    public TMP_InputField buildingNameInput;
    public TMP_InputField descriptionInput;
    public TMP_InputField latitudeInput;
    public TMP_InputField longitudeInput;

    [Header("Titles for Required Fields (Panel1)")]
    public TMP_Text buildingNameTitle;         
    public TMP_Text latitudeTitle;         
    public TMP_Text longitudeTitle;        

    [Header("Image Preview")]
    public Image previewImage;

    [Header("Buttons")]
    public Button markLocationButton; 
    public Button cameraButton;       
    public Button galleryButton;
    public Button nextButton;  
    public Button saveButton;  

    [Header("Campus Map (Panel2)")]
    public RectTransform campusMapRect;    
    public GameObject markerPrefab;        
    private GameObject currentMarker;

    // Firestore/Storage
    private FirebaseFirestore db;
    private StorageReference storageRef;

   
    private string currentDocId = "";
    private string existingImageUrl = "";

    private Texture2D selectedTexture;
    private string imagePath = "";

    
    private float mapX = -1f;
    private float mapY = -1f;

    public TMP_Text statusText;

    public TMP_Text buildingNameErrorText;
    public TMP_Text latitudeErrorText;
    public TMP_Text longitudeErrorText;

    [Header("CampusMap")]
    public Image campusMapImage;

    public GameObject loadingIndicator; 


    

    void Start()
    {
        
        db = FirebaseFirestore.DefaultInstance;
        storageRef = FirebaseStorage.DefaultInstance.RootReference;
        campusDocId = SessionData.Instance.CampusId;
        adminid = SessionData.Instance.AdminId;

        
        LoadAllBuildingMarkers();

        
        if (markLocationButton != null)
            markLocationButton.onClick.AddListener(OnMarkLocationClicked);// Mark Location button uses GPS to fill latitude/longitude

        if (cameraButton != null)
            cameraButton.onClick.AddListener(OnTakePhotoClicked);// Camera button opens the camera to take a photo

        if (galleryButton != null)
            galleryButton.onClick.AddListener(OnSelectFromGalleryClicked);// Gallery button opens the image picker

        
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);// Next button starts the next panel

        if (saveButton != null)
            saveButton.onClick.AddListener(() => StartCoroutine(OnSaveClicked()));// Save button starts the save coroutine


        
        panel1.SetActive(true);
        panel2.SetActive(false);

        if (!string.IsNullOrEmpty(BuildingDataHolder.docId))
        {
            OpenForEdit(BuildingDataHolder.docId);
            BuildingDataHolder.docId = "";
        }
        else
        {
            BuildingDataHolder.docId = ""; 
            OpenForAdd();
        }

        LoadCampusMap();
    }

    private void DeletePlaceholderIfExists()
    {
        DocumentReference phRef = db.Collection("Campuses")
                                    .Document(campusDocId)
                                    .Collection("Buildings")
                                    .Document("_placeholder");

        phRef.DeleteAsync();


    }// Deletes the placeholder document if it exists
    
    // --- ADD / EDIT Modes ---
    public void OpenForAdd()
    {
        ClearFields();
        BuildingDataHolder.docId = "";
        currentDocId = "";
        existingImageUrl = "";

    }// Clears fields and sets up for adding a new building

    public void OpenForEdit(string documentId)
    {
        currentDocId = documentId;
        LoadExistingBuildingData(documentId);
    }// Loads existing building data for editing
    
    private void LoadExistingBuildingData(string docId)
    {
        DocumentReference docRef = db.Collection("Campuses").Document(campusDocId).Collection("Buildings").Document(docId);// Reference to the specific building document
        docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error loading building data: " + task.Exception);
                return;
            }
            var snapshot = task.Result;
            if (!snapshot.Exists)
            {
                Debug.LogWarning("No building found with ID: " + docId);
                return;
            }
            var data = snapshot.ToDictionary();

            
            buildingNameInput.text = data.ContainsKey("buildingName") ? data["buildingName"].ToString() : "";
            descriptionInput.text  = data.ContainsKey("description")  ? data["description"].ToString()  : "";

            if (data.ContainsKey("mapX") && data.ContainsKey("mapY"))
            {
                mapX = Convert.ToSingle(data["mapX"]);
                mapY = Convert.ToSingle(data["mapY"]);
                
            }
            else
            {
                mapX = -1f;
                mapY = -1f;
            }

            if (data.ContainsKey("coordinates"))
            {
                string coords = data["coordinates"].ToString();
                var parts = coords.Split(',');
                if (parts.Length == 2)
                {
                    string lat = parts[0].Trim().Split(' ')[0]; 
                    string lon = parts[1].Trim().Split(' ')[0]; 
                    latitudeInput.text  = lat;
                    longitudeInput.text = lon;
                }// If coordinates are in the expected format, split and set latitude/longitude
                else
                {
                    latitudeInput.text  = "";
                    longitudeInput.text = "";
                }
            }
            else
            {
                latitudeInput.text  = "";
                longitudeInput.text = "";
            }

            
            if (data.ContainsKey("imageURL"))
            {
                existingImageUrl = data["imageURL"].ToString();
                if (!string.IsNullOrEmpty(existingImageUrl))
                {
                    StartCoroutine(LoadImageFromUrl(existingImageUrl, previewImage));// Load existing image from URL
                }
            }
            else
            {
                existingImageUrl = "";
            }
            Debug.Log("Loaded existing building data for editing.");
        });
    }// Loads existing building data from Firestore for editing

    
    private void ClearFields()
    {
        buildingNameInput.text = "";
        descriptionInput.text  = "";
        latitudeInput.text     = "";
        longitudeInput.text    = "";
        previewImage.sprite    = null;
        selectedTexture        = null;
        imagePath              = "";
        if (currentMarker != null)
        {
            Destroy(currentMarker);
            currentMarker = null;
        }
    }// Clears all input fields and resets the marker

    // ---------------- PANEL1: כפתור Next ----------------
    private void OnNextClicked()
    {
        
        bool isValid = CheckPanel1Fields();
        if (!isValid)
        {
            ShowStatusKey("fill_all_fields", true);
            
            return;
        }

      
        panel1.SetActive(false);
        panel2.SetActive(true);

        if (mapX >= 0 && mapY >= 0)
        {
            PlaceMarker(new Vector2(mapX, mapY));
        }
        ShowStatusKey("please_wait", false); 
        
    }// Validates fields in Panel1, switches to Panel2 if valid

    private bool CheckPanel1Fields()
    {
        bool isValid = true;

        // 1) Building Name
        if (string.IsNullOrEmpty(buildingNameInput.text.Trim()))
        {
            HighlightField(buildingNameInput, true);
            if (buildingNameErrorText != null)
            {
                L10n.SetText(buildingNameErrorText, "err_building_name");
                buildingNameErrorText.gameObject.SetActive(true);
            }
            isValid = false;
        }
        else
        {
            HighlightField(buildingNameInput, false);
            if (buildingNameErrorText != null)
            {
                buildingNameErrorText.gameObject.SetActive(false);
            }
        }

        // 2) Latitude
        if (string.IsNullOrEmpty(latitudeInput.text.Trim()))
        {
            HighlightField(latitudeInput, true);
            if (latitudeErrorText != null)
            {   
                L10n.SetText(latitudeErrorText, "lat_required");
                latitudeErrorText.text = "Latitude is required";
                latitudeErrorText.gameObject.SetActive(true);
            }
            isValid = false;
        }
        else
        {
            HighlightField(latitudeInput, false);
            if (latitudeErrorText != null)
            {
                latitudeErrorText.gameObject.SetActive(false);
            }
        }

        // 3) Longitude
        if (string.IsNullOrEmpty(longitudeInput.text.Trim()))
        {
            HighlightField(longitudeInput, true);
            if (longitudeErrorText != null)
            {   
                L10n.SetText(longitudeErrorText, "lon_required");
                longitudeErrorText.text = "Longitude is required";
                longitudeErrorText.gameObject.SetActive(true);
            }
            isValid = false;
        }
        else
        {
            HighlightField(longitudeInput, false);
            if (longitudeErrorText != null)
            {
                longitudeErrorText.gameObject.SetActive(false);
            }
        }

        return isValid;
    }// Validates fields in Panel1 and highlights errors




    
    private IEnumerator OnSaveClicked()
    {
        ShowStatusKey("please_wait", false); 
        if (mapX < 0f || mapY < 0f)
        {
            ShowStatusKey("select_location_first", true);
           
            yield break;

        }

        StartCoroutine(CreateOrUpdateBuildingRoutine());
        
    }// Coroutine to handle the save button click, validates fields, and starts the save process

    
    public void OnCampusMapClicked(BaseEventData eventData)
    {
        PointerEventData ped = (PointerEventData)eventData;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            campusMapRect,
            ped.position,
            ped.pressEventCamera,
            out localPoint))
        {
            // Convert local coordinates => normalized
            Vector2 normalized = LocalToNormalized(localPoint);
            mapX = normalized.x;
            mapY = normalized.y;

            Debug.Log($"Campus map clicked: local={localPoint}, normalized=({mapX:F3}, {mapY:F3})");
            PlaceMarker(normalized);
        }
    }// Handles clicks on the campus map, converts local coordinates to normalized, and places a marker

    private Vector2 LocalToNormalized(Vector2 localPoint)
    {
        float width = campusMapRect.rect.width;
        float height = campusMapRect.rect.height;
        float nx = (localPoint.x / width) + 0.5f;
        float ny = (localPoint.y / height) + 0.5f;
        return new Vector2(Mathf.Clamp01(nx), Mathf.Clamp01(ny));
    }// Converts local coordinates to normalized coordinates (0-1 range)

    private void PlaceMarker(Vector2 normalized)
    {
        if (markerPrefab == null) return;
        if (currentMarker == null)
        {
            currentMarker = Instantiate(markerPrefab, campusMapRect);
        }
        RectTransform markerRect = currentMarker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            float localX = (normalized.x - 0.5f) * campusMapRect.rect.width;
            float localY = (normalized.y - 0.5f) * campusMapRect.rect.height;
            markerRect.anchoredPosition = new Vector2(localX, localY);
        }
    }// Places a marker on the campus map at the specified normalized coordinates

    // ---------------- GPS ----------------
    // Button click handler to start the GPS location filling coroutine
public void OnMarkLocationClicked()
    {
        StartCoroutine(FillCurrentLocationAccurate());
    }

    // Coroutine to fill the current GPS location with high accuracy
IEnumerator FillCurrentLocationAccurate()
{
#if UNITY_ANDROID
    if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
    {
        Permission.RequestUserPermission(Permission.FineLocation);
        yield return null;
    }
#endif

    if (!Input.location.isEnabledByUser)
    {
        Debug.LogWarning("GPS not enabled or permission denied.");
        yield break;
    }

    
    if (loadingIndicator != null)
        loadingIndicator.SetActive(true);

    Input.location.Start(1f, 0f);

    float startupTimeout = 20f;
    while (Input.location.status == LocationServiceStatus.Initializing && startupTimeout > 0f)
    {
        yield return new WaitForSeconds(1f);
        startupTimeout -= 1f;
    }

    if (Input.location.status != LocationServiceStatus.Running)
    {
        Debug.LogError("Unable to start location service.");
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        yield break;
    }

    float desiredAcc  = 5f;
    float waitTimeout = 7f;
    while (Input.location.lastData.horizontalAccuracy > desiredAcc && waitTimeout > 0f)
    {
        yield return new WaitForSeconds(0.25f);
        waitTimeout -= 0.25f;
    }

    var last = Input.location.lastData;
    float lat = last.latitude;
    float lon = last.longitude;

    latitudeInput.text = lat.ToString("F6");// Set latitude and longitude inputs with 6 decimal places
    longitudeInput.text = lon.ToString("F6");// Set latitude and longitude inputs with 6 decimal places

    Input.location.Stop();

    
    if (loadingIndicator != null)
        loadingIndicator.SetActive(false);
}

    // ---------------- CAMERA / GALLERY ----------------
    // Button click handler to open the camera and take a photo
public void OnTakePhotoClicked()
    {
        NativeCamera.TakePicture((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                LoadAndDisplayImage(path);
            }

            else
            {
                Debug.LogWarning("No picture taken or permission denied.");
            }

        }, 1024); // 1024 is the max size

    }

 // Button click handler to open the gallery and select an image
public void OnSelectFromGalleryClicked()
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

    }, "Select Image", "image/*");
}

 
// Loads an image from the specified path and displays it in the preview image
    void LoadAndDisplayImage(string path)
    {
        Texture2D tex = NativeGallery.LoadImageAtPath(path, 1024, false, false);
        if (tex != null)
        {
            selectedTexture = tex;
            imagePath = path;
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            previewImage.sprite = sp;
        }
    }

    // ---------------- CREATE / UPDATE BUILDING ----------------
    // Coroutine to create or update a building document in Firestore
IEnumerator CreateOrUpdateBuildingRoutine()
{
    string buildingName = buildingNameInput.text;
    string desc         = descriptionInput.text;
    string coordinates  = $"{latitudeInput.text} N, {longitudeInput.text} E";

  
    var buildingData = new Dictionary<string, object>
    {
        { "buildingName", buildingName },
        { "description",  desc },
        { "updatedAt",    Timestamp.GetCurrentTimestamp() }, // ⏰ תמיד נוסיף updatedAt
        { "coordinates",  coordinates },
        { "mapX",         mapX },
        { "mapY",         mapY },
        { "imageURL",     existingImageUrl }
    };
   
    

   
    if (string.IsNullOrEmpty(currentDocId))
    {   
        
        buildingData.Add("createdAt", Timestamp.GetCurrentTimestamp());
    }

    
    if (selectedTexture != null)
    {
        yield return UploadImageToFirebaseStorage(selectedTexture, (downloadUrl) =>
        {
            buildingData["imageURL"] = downloadUrl;
        });
    }

   
    if (string.IsNullOrEmpty(currentDocId))
    {
        
        DeletePlaceholderIfExists();

        DocumentReference newDocRef = db.Collection("Campuses")
                                        .Document(campusDocId)
                                        .Collection("Buildings")
                                        .Document();

        var setTask = newDocRef.SetAsync(buildingData);
        yield return new WaitUntil(() => setTask.IsCompleted);

        if (setTask.Exception != null)
        {
            ShowStatusKey("err_add_prefix" + setTask.Exception, true);
            
        }
        else
        {
            LoadAllBuildingMarkers();
            ShowStatusKey("success_building_saved", false);

            
            currentDocId = newDocRef.Id;
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene("MyBuildings");
        }
    }

    else
    {
        // If currentDocId is not empty, update the existing document
        // EDIT
            DocumentReference docRef = db.Collection("Campuses").Document(campusDocId).Collection("Buildings").Document(currentDocId);
        var setTask = docRef.SetAsync(buildingData, SetOptions.MergeAll);
        yield return new WaitUntil(() => setTask.IsCompleted);

        if (setTask.Exception != null)
        {
            ShowStatusKey("err_add_prefix" + setTask.Exception, true);
            
        }
        else
        {
            ShowStatusKey("success_building_edit", false);
            LoadAllBuildingMarkers();
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene("MyBuildings");
        }
    }
}

// Coroutine to upload the image to Firebase Storage and get the download URL
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
            Debug.Log("Image Download URL: " + downloadUrl);
            onComplete(downloadUrl);
        }
    }

    

    private IEnumerator LoadImageFromUrl(string url, Image targetImage)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                Sprite sp = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );
                targetImage.sprite = sp;
            }
            else
            {
                Debug.LogError("Error loading image from URL: " + request.error);
            }
        }
    }



    public Color successColor = new Color32(0x60, 0x7A, 0xFB, 0xFF);
    public Color errorColor   = Color.red;
    // ---------------- STATUS / MESSAGES ----------------
    private void ShowStatus(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.color = isError ? errorColor : successColor;
            statusText.text  = message;
        }
        Debug.Log(message);
    }

    private void ShowStatusKey(string key, bool isError)
    {

        statusText.color = isError ? errorColor : successColor;

        L10n.SetText(statusText, key);

    }
 



    // ---------------- LOAD MARKERS + PLACE ON MAP ----------------
    public void LoadAllBuildingMarkers()
    {
        // Clears old markers
        foreach (Transform child in campusMapRect)
        {
            Destroy(child.gameObject);
        }

        // Fetch
        db.Collection("Campuses").Document(campusDocId).Collection("Buildings").GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error loading building markers: " + task.Exception);
                return;
            }

            QuerySnapshot snapshot = task.Result;
            Debug.Log($"Found {snapshot.Count} building documents.");

            // For each doc with mapX,mapY => place a pin
            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                Dictionary<string, object> data = doc.ToDictionary();
                if (data.ContainsKey("mapX") && data.ContainsKey("mapY"))
                {
                    float x = Convert.ToSingle(data["mapX"]);
                    float y = Convert.ToSingle(data["mapY"]);
                    Vector2 normalized = new Vector2(x, y);
                    PlaceMarkerForBuilding(doc.Id, normalized);
                }
            }
        });
    }

    private void PlaceMarkerForBuilding(string docId, Vector2 normalized)
    {
        if (markerPrefab == null) return;
        GameObject marker = Instantiate(markerPrefab, campusMapRect);
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        if (markerRect != null)
        {
            float localX = (normalized.x - 0.5f) * campusMapRect.rect.width;
            float localY = (normalized.y - 0.5f) * campusMapRect.rect.height;
            markerRect.anchoredPosition = new Vector2(localX, localY);
        }
        
    }

    // ---------------- HIGHLIGHT FIELDS ----------------
 
    private void HighlightField(TMP_InputField input, bool highlight)
    {
    
        Color normalBg   = new Color(0.863f, 0.867f, 0.859f); //   (#DCDCDB)
        Color errorBg    = new Color(1f, 0.933f, 0.933f);     //  (#FFEDED)

    
        Color normalOutline = new Color(0.839f, 0.855f, 0.859f); //   (#D6DADB)
        Color errorOutline  = new Color(0.945f, 0.584f, 0.596f); //  (#F19598)

    
        Color normalTitle = Color.black;
        Color errorTitle  = new Color(1f, 0.078f, 0f); // #FF0800

    
        Image bg = input.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = highlight ? errorBg : normalBg;
        }

    
        Outline outline = input.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = highlight ? errorOutline : normalOutline;
        }

    
        if (input == buildingNameInput && buildingNameTitle != null)
        {
            buildingNameTitle.color = highlight ? errorTitle : normalTitle;
        }
        else if (input == latitudeInput && latitudeTitle != null)
        {
            latitudeTitle.color = highlight ? errorTitle : normalTitle;
        }
        else if (input == longitudeInput && longitudeTitle != null)
        {
            longitudeTitle.color = highlight ? errorTitle : normalTitle;
        }
    }


   private void LoadCampusMap()
{
    DocumentReference campusRef = db.Collection("Campuses").Document(campusDocId);

    campusRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
    {
        if (task.IsFaulted || task.IsCanceled)
        {
            Debug.LogError("Failed to load campus doc: " + task.Exception);
            return;
        }

        var snapshot = task.Result;
        if (!snapshot.Exists)
        {
            Debug.LogWarning("No campus document found");
            return;
        }

        Dictionary<string, object> data = snapshot.ToDictionary();

        if (data.TryGetValue("mapImageURL", out object urlObj))
        {
            string url = urlObj.ToString();
            Debug.Log("Map URL: " + url);
            StartCoroutine(LoadMapImage(url, campusMapImage));
        }
        else
        {
            Debug.LogWarning("No mapImageURL field in campus document");
        }
    });
}

IEnumerator LoadMapImage(string url, Image image)
{
    using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
    {
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            image.sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            image.preserveAspect = false;

            
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



