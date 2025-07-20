using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System;

/// <summary>
/// Handles the process for users requesting management access to an existing campus.
/// Collects all required inputs, attaches files, sends a multipart POST request to the backend,
/// and manages UI feedback (loading spinner and success popup).
/// Used in: Request to Manage screen.
/// </summary>

public class RequestManageHandler : MonoBehaviour
{

    /* ────────── INPUT FIELDS ────────── */
    [Header("Admin Info Inputs")]
    public TMP_InputField adminNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField roleInput;

    /* ────────── FILE PATHS ────────── */
    private string approvalFilePath;
    private string profilePicturePath;

    public void SetApprovalPath(string path) => approvalFilePath = path;
    public void SetProfilePhotoPath(string path) => profilePicturePath = path;

    /* ────────── UI FEEDBACK ────────── */
    [Header("UI Feedback")]
    public GameObject loadingSpinner;
    [SerializeField] private GameObject successPopupOverlay;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private Button okButton;

    [Header("Campus Dropdown")]
    public SearchDropdown campusDropdown;

    [Header("Shared Validator")]
    public RegistrationValidator registrationValidator;


    /* ────────── CONFIG ────────── */
    private const string cloudFunctionUrl =
        "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/requestManage/requestManage";


    private void Awake()
    {
        okButton.onClick.AddListener(() => StartCoroutine(FadeOutPopup()));     
        var btnColors = okButton.colors;
        btnColors.normalColor = new Color32(46, 204, 113, 255);
        btnColors.highlightedColor = new Color32(39, 174, 96, 255);
        okButton.colors = btnColors;
    }

    private void Start()
    {
        LoadCampusNames();
        registrationValidator.requireCampusName = true;
        registrationValidator.campusNameInput = campusDropdown.searchInput;
    }

    private async void LoadCampusNames()
    {
        Debug.Log("Loading campus names...");
        var campusNames = await CampusFetcher.GetCampusNames();

        if (campusNames.Count == 0)
        {
            Debug.LogWarning("No campuses found or error fetching.");
            return;
        }

        campusDropdown.SetOptions(campusNames);
    }


    /* ---------- called by the SEND button ---------- */
    public void OnSubmitClicked() => StartCoroutine(SendAddManagerRequest());

    /* ────────── main coroutine ────────── */
    private IEnumerator SendAddManagerRequest()
    {
        // Validation
        string campusName = campusDropdown.searchInput.text.Trim();

        if (string.IsNullOrWhiteSpace(adminNameInput.text) ||
            string.IsNullOrWhiteSpace(emailInput.text) ||
            string.IsNullOrWhiteSpace(passwordInput.text) ||
            string.IsNullOrWhiteSpace(roleInput.text) ||
            string.IsNullOrWhiteSpace(campusName))
        {
            Debug.LogWarning("Fill all required fields.");
            yield break;
        }

        loadingSpinner.SetActive(true);

        // Build form
        WWWForm form = new WWWForm();
        form.AddField("campusName", campusName);
        form.AddField("email", emailInput.text.Trim());
        form.AddField("password", passwordInput.text);
        form.AddField("adminName", adminNameInput.text.Trim());
        form.AddField("role", roleInput.text.Trim());

        // Optional files
        if (File.Exists(approvalFilePath))
        {
            string ext = Path.GetExtension(approvalFilePath).ToLower();
            string mime = ext == ".pdf" ? "application/pdf" : "application/octet-stream";
            AddFileToForm(form, "approval", approvalFilePath, mime);
        }
        if (File.Exists(profilePicturePath))
        {
            string ext = Path.GetExtension(profilePicturePath).ToLower();
            string mime = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };
            AddFileToForm(form, "profilePhoto", profilePicturePath, mime);
        }

        // Send request
        using (UnityWebRequest request = UnityWebRequest.Post(cloudFunctionUrl, form))
        {
            // No authentication header for guests
            yield return request.SendWebRequest();
            loadingSpinner.SetActive(false);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("RequestManage failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.Log("RequestManage success: " + request.downloadHandler.text);
                StartCoroutine(FadeInPopup());
            }
        }
    }


    /* ────────── helpers ────────── */

    private void AddFileToForm(WWWForm form, string field, string filePath, string mime)
    {
        byte[] data = File.ReadAllBytes(filePath);
        string name = Path.GetFileName(filePath);
        form.AddBinaryData(field, data, name, mime);
    }



    private IEnumerator FadeInPopup()
    {
        CanvasGroup cg = successPopupOverlay.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        successPopupOverlay.SetActive(true);
        while (cg.alpha < 1f) { cg.alpha += Time.deltaTime * 2; yield return null; }
    }

    private IEnumerator FadeOutPopup()
    {
        CanvasGroup cg = successPopupOverlay.GetComponent<CanvasGroup>();
        while (cg.alpha > 0f) { cg.alpha -= Time.deltaTime * 2; yield return null; }
        successPopupOverlay.SetActive(false);
    }
}

/*
    --- Key Concepts ---

    - Campus selection: Only "active" campuses are loaded and selectable (via CampusFetcher).
    - MIME type: Each file upload specifies a MIME type, informing the server of its format (e.g., "application/pdf", "image/jpeg").
    - Validator usage: Relies on RegistrationValidator for field validation and error feedback.
    - No authentication: This request is intended for guest/registration use, no login token attached.

    --- Logic Summary ---

    - Collects admin and campus input, validates required fields, attaches files if present, and submits to the backend.
    - Handles loading and feedback UI for a smooth user experience.
    - Used exclusively on the Request to Manage screen.
*/