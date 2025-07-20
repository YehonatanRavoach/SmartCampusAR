using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using Firebase.Firestore;
using Firebase.Extensions;

/// <summary>
/// UI controller for one administrator row.  Responsible for:
/// • Setting up text, images, and event listeners.<br/>
/// • Updating admin status via <c>setAdminStatus</c> Cloud Function.<br/>
/// • Deleting admin profile via <c>deleteAdmin</c> Cloud Function.<br/>
/// • Ensuring dropdown state matches server state only after success.
/// </summary>
public class AdminItem : MonoBehaviour
{
    public TMP_Dropdown statusDropdown;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI campusNameText;
    public Button openInBrowserButton;
    public Image adminImage;
    public Button statusButton;
    public Button deleteButton;
    public GameObject loadingSpinner;
    [SerializeField] private GameObject confirmDeletePrefab;

    private AdminListManager manager;
    private string docId;
    private string campusId;
    private string approvalFileUrl;
    private string originalStatus;
    private int previousStatusIndex;

    // Cloud Function URLs
    private const string setAdminStatusUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/setAdminStatus";
    private const string deleteAdminUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/deleteAdmin";

    /// <summary>
    /// Initializes the admin item with its data and sets up all UI handlers.
    /// </summary>
    public void Setup(AdminListManager managerRef, AdminDataLocal data)
    {
        manager = managerRef;
        docId = data.docId;
        campusId = data.campusId;
        approvalFileUrl = data.employeeApprovalFileURL;

        if (nameText != null) nameText.text = data.adminName;
        if (emailText != null) emailText.text = data.email;
        if (openInBrowserButton != null && !string.IsNullOrEmpty(approvalFileUrl))
        {
            openInBrowserButton.onClick.RemoveAllListeners();
            openInBrowserButton.onClick.AddListener(() =>
                Application.OpenURL(approvalFileUrl)
            );
        }

        if (adminImage != null && !string.IsNullOrEmpty(data.adminPhotoURL))
            StartCoroutine(LoadImage(data.adminPhotoURL, adminImage));

        LoadCampusName(campusId);

        // Setup status dropdown and save the original index
        if (statusDropdown != null)
        {
            string statusLower = data.status.ToLower();
            int found = statusDropdown.options.FindIndex(
                o => o.text.ToLower() == statusLower
            );
            if (found >= 0)
            {
                statusDropdown.value = found;
                previousStatusIndex = found;
                statusDropdown.RefreshShownValue();
                originalStatus = data.status.ToLower();
            }
            else
            {
                Debug.LogWarning($"Status '{data.status}' not found in dropdown options");
            }
        }

        // Status update button logic (delays UI update until server approval)
        if (statusDropdown != null && statusButton != null)
        {
            statusButton.interactable = false;

            statusDropdown.onValueChanged.RemoveAllListeners();
            statusDropdown.onValueChanged.AddListener(_ =>
            {
                string selected = statusDropdown.options[statusDropdown.value].text.ToLower();
                statusButton.interactable =
                    !selected.Equals(originalStatus, System.StringComparison.OrdinalIgnoreCase);
            });

            statusButton.onClick.RemoveAllListeners();
            statusButton.onClick.AddListener(() =>
            {
                string selectedStatus = statusDropdown.options[statusDropdown.value].text.ToLower();
                // Immediately disable dropdown and button to prevent changes mid-request
                statusDropdown.interactable = false;
                statusButton.interactable = false;
                StartCoroutine(SendSetAdminStatusRequest(selectedStatus));
            });
        }

        // Delete button logic
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(OnDeleteAdminClicked);
        }
    }

    /// <summary>
    /// Sends a WebRequest to the Cloud Function to update the admin's status.
    /// Dropdown/status will only be updated upon success. On failure, restores the previous value.
    /// </summary>
    private IEnumerator SendSetAdminStatusRequest(string newStatus)
    {
        if (loadingSpinner != null) loadingSpinner.SetActive(true);

        string idToken = SessionData.Instance.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogError("ID token not found. Ensure user is logged in.");
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
            statusDropdown.interactable = true;
            yield break;
        }

        string jsonBody = "{\"data\": {\"adminId\": \"" + docId + "\", \"newStatus\": \"" + newStatus + "\"}}";

        using (UnityWebRequest request = new UnityWebRequest(setAdminStatusUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("setAdminStatus succeeded: " + request.downloadHandler.text);
                originalStatus = newStatus;
                previousStatusIndex = statusDropdown.value;
                statusButton.interactable = false;
                manager.LoadAllAdminsFromFirestore();
            }
            else
            {
                Debug.LogError("setAdminStatus failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);

                // Restore dropdown to previous value
                statusDropdown.value = previousStatusIndex;
                statusDropdown.RefreshShownValue();

                // Optionally show popup to user
            }
            statusDropdown.interactable = true;
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    /// <summary>
    /// Opens confirmation dialog and sends WebRequest to delete the admin via Cloud Function.
    /// </summary>
    private void OnDeleteAdminClicked()
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
            Debug.LogError("confirmDeletePrefab missing ConfirmationDialog script.");
            return;
        }

        L10n.Get("confirm_delete_admin", localizedText =>
        {
            dialog.Initialize(
                localizedText,
                () =>
                {
                    if (loadingSpinner != null) loadingSpinner.SetActive(true);
                    StartCoroutine(SendDeleteAdminRequest());
                },
                () => Debug.Log("Delete cancelled")
            );
        });
    }

    /// <summary>
    /// Sends a WebRequest to the Cloud Function to delete the admin.
    /// </summary>
    private IEnumerator SendDeleteAdminRequest()
    {
        if (string.IsNullOrEmpty(docId))
        {
            Debug.LogWarning("Admin ID is empty.");
            yield break;
        }

        string idToken = SessionData.Instance.IdToken;
        if (string.IsNullOrEmpty(idToken))
        {
            Debug.LogError("ID token is missing. Are you logged in?");
            yield break;
        }

        string jsonBody = "{\"data\": {\"adminId\": \"" + docId + "\"}}";

        using (UnityWebRequest request = new UnityWebRequest(deleteAdminUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Admin deleted successfully: " + request.downloadHandler.text);
                manager.LoadAllAdminsFromFirestore();
            }
            else
            {
                Debug.LogError("Admin deletion failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            if (loadingSpinner != null) loadingSpinner.SetActive(false);
        }
    }

    /// <summary>
    /// Loads the campus name for display.
    /// </summary>
    private void LoadCampusName(string campusId)
    {
        var db = FirebaseFirestore.DefaultInstance;
        db.Collection("Campuses").Document(campusId).GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Error loading campus: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
            if (snapshot.Exists && snapshot.TryGetValue("name", out string campusName))
            {
                if (campusNameText != null)
                    campusNameText.text = campusName;
            }
        });
    }

    /// <summary>
    /// Loads the admin image from a given URL.
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
                Debug.LogError("Failed to load admin image: " + req.error);
            }
        }
    }
}
