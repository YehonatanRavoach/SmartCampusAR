using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;

/*
 * CampusItem.cs
 * -----------------------------------------------------------------------------
 * Represents a single campus entry in the list UI. Handles displaying campus
 * data, updating status via Cloud Function, and deleting the campus after
 * user confirmation. Data is sent using HTTP WebRequests.
 */
public class CampusItem : MonoBehaviour
{
    private CampusListManager manager;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI cityText;
    public TextMeshProUGUI countryText;
    public TextMeshProUGUI descriptionText;
    public Image logoImage;
    public Button deleteButton;
    [SerializeField] private GameObject confirmDeletePrefab;
    public GameObject loadingSpinner;
    [Header("Status")]
    public TMP_Dropdown statusDropdown;
    public Button updateButton;
    private string docId;
    private string originalStatus;

    // Cloud Function URLs
    private const string setCampusStatusUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/setCampusStatus";// URL to update campus status
    private const string deleteCampusUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/deleteCampus";// URL to delete campus

    public void Setup(CampusListManager managerRef, CampusData data)
    {
        manager = managerRef;
        docId = data.docId;

        if (nameText != null) nameText.text = data.name;
        if (cityText != null) cityText.text = data.city;
        if (countryText != null) countryText.text = data.country;
        if (descriptionText != null) descriptionText.text = data.description;

        // Load logo image if exists
        if (!string.IsNullOrEmpty(data.logoURL))
            StartCoroutine(LoadImage(data.logoURL, logoImage));

        // Setup status dropdown
        originalStatus = data.status;
        if (statusDropdown != null)
        {
            statusDropdown.onValueChanged.RemoveAllListeners();

            int idx = statusDropdown.options
                     .FindIndex(o => o.text.Equals(data.status, StringComparison.OrdinalIgnoreCase));
            statusDropdown.value = Mathf.Max(0, idx);
            statusDropdown.RefreshShownValue();

            // Enable update button only on change
            statusDropdown.onValueChanged.AddListener(_ =>
            {
                string selected = statusDropdown.options[statusDropdown.value].text;
                updateButton.interactable =
                    !selected.Equals(originalStatus, StringComparison.OrdinalIgnoreCase);
            });
        }

        // Setup update status button
        if (updateButton != null)
        {
            updateButton.onClick.RemoveAllListeners();
            updateButton.onClick.AddListener(OnUpdateCampusClicked);
            updateButton.interactable = false;
        }

        // Setup delete button
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteCampusClicked);
        }
    }

    /// <summary>
    /// Handles campus status update via Cloud Function WebRequest.
    /// </summary>
    private void OnUpdateCampusClicked()
    {
        string newStatus = statusDropdown.options[statusDropdown.value].text;
        StartCoroutine(SendSetCampusStatusRequest(docId, newStatus));
    }

    private IEnumerator SendSetCampusStatusRequest(string campusId, string newStatus)
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(true);

        string idToken = SessionData.Instance.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogError("ID token not found. Ensure user is logged in.");
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
            yield break;
        }

        string jsonBody = "{\"data\": {\"campusId\": \"" + campusId + "\", \"newStatus\": \"" + newStatus.ToLower() + "\"}}";

        using (UnityWebRequest request = new UnityWebRequest(setCampusStatusUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("setCampusStatus succeeded: " + request.downloadHandler.text);
                originalStatus = newStatus;
                updateButton.interactable = false;
                manager.LoadCampuses();
            }
            else
            {
                Debug.LogError("setCampusStatus failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    /// <summary>
    /// Opens confirmation dialog and sends WebRequest to delete the campus via Cloud Function.
    /// </summary>
    private void OnDeleteCampusClicked()
    {
        if (confirmDeletePrefab == null)
        {
            Debug.LogWarning("confirmDeletePrefab is not assigned.");
            return;
        }

        GameObject dialogGO = Instantiate(confirmDeletePrefab, transform.root);
        ConfirmationDialog dialog = dialogGO.GetComponentInChildren<ConfirmationDialog>();

        if (dialog == null)
        {
            Debug.LogError("Missing ConfirmationDialog script.");
            return;
        }

        L10n.Get("confirm_delete_campus", localizedText =>
        {
            dialog.Initialize(
                localizedText,
                () =>
                {
                    if (loadingSpinner != null) loadingSpinner.SetActive(true);
                    StartCoroutine(SendDeleteCampusRequest());
                },
                () => Debug.Log("Delete cancelled")
            );
        });
    }

    private IEnumerator SendDeleteCampusRequest()
    {
        if (string.IsNullOrEmpty(docId))
        {
            Debug.LogWarning("Campus ID is empty.");
            yield break;
        }

        string idToken = SessionData.Instance.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogError("ID token not found. Ensure user is logged in.");
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
            yield break;
        }

        string jsonBody = "{\"data\": {\"campusId\": \"" + docId + "\"}}";

        using (UnityWebRequest request = new UnityWebRequest(deleteCampusUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Campus deleted successfully: " + request.downloadHandler.text);
                manager.LoadCampuses();
            }
            else
            {
                Debug.LogError("Campus deletion failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    /// <summary>
    /// Loads the logo image from a given URL.
    /// </summary>
    private IEnumerator LoadImage(string url, Image targetImage)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = ((DownloadHandlerTexture)req.downloadHandler).texture;
                targetImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogWarning("Image load failed: " + req.error);
            }
        }
    }
}
