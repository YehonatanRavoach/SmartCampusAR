using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Handles the registration submission process for a new admin and campus.
/// Collects all form fields and uploaded files, sends them as a multipart POST request to the backend,
/// and manages UI feedback (loading spinner and success popup).
/// Used in: Create a New Account (Submit button, final step)
/// </summary>
public class AdminRegistrationHandler : MonoBehaviour
{
    [Header("Admin Info Inputs")]
    public TMP_InputField adminNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField roleInput;

    [Header("Campus Info Inputs")]
    public TMP_InputField campusNameInput;
    public TMP_InputField descriptionInput;

    private string logoFilePath;
    private string mapFilePath;
    private string approvalFilePath;
    private string profilePicturePath;

    public CountryCityDropdownController dropdownController;

    [Header("UI Feedback")]
    public GameObject loadingSpinner;

    [SerializeField] private GameObject successPopupOverlay;
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private Button okButton;

    private string cloudFunctionUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/registerAdmin/registerAdmin";

    public void SetLogoPath(string path) => logoFilePath = path;
    public void SetMapPath(string path) => mapFilePath = path;
    public void SetApprovalPath(string path) => approvalFilePath = path;
    public void SetProfilePhotoPath(string path) => profilePicturePath = path;

    private void Awake()
    {
        okButton.onClick.AddListener(() => StartCoroutine(FadeOutPopup()));

        var btnColors = okButton.colors;
        btnColors.normalColor = new Color32(46, 204, 113, 255);
        btnColors.highlightedColor = new Color32(39, 174, 96, 255);
        okButton.colors = btnColors;
    }

    public void OnSubmitClicked()
    {
        StartCoroutine(SendRegistrationRequest());
    }

    private IEnumerator SendRegistrationRequest()
    {
        loadingSpinner.SetActive(true);

        string country = dropdownController.GetSelectedCountry();
        string city = dropdownController.GetSelectedCity();

        WWWForm form = new WWWForm();
        form.AddField("email", emailInput.text);
        form.AddField("password", passwordInput.text);
        form.AddField("adminName", adminNameInput.text);
        form.AddField("role", roleInput.text);
        form.AddField("campusName", campusNameInput.text);
        form.AddField("description", descriptionInput.text);
        form.AddField("country", country);
        form.AddField("city", city);

        TryAddFile(form, "logo", logoFilePath, "image/jpeg");
        TryAddFile(form, "map", mapFilePath, "image/jpeg");
        TryAddFile(form, "approval", approvalFilePath, DetectMimeType(approvalFilePath));
        TryAddFile(form, "profilePicture", profilePicturePath, DetectMimeType(profilePicturePath));

        using (UnityWebRequest request = UnityWebRequest.Post(cloudFunctionUrl, form))
        {
            yield return request.SendWebRequest();
            loadingSpinner.SetActive(false);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Upload failed: " + request.error);
                Debug.Log("Response: " + request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Upload success: " + request.downloadHandler.text);
                StartCoroutine(FadeInPopup());
            }
        }
    }

    private void TryAddFile(WWWForm form, string fieldName, string filePath, string mimeType)
    {
        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            byte[] fileData = File.ReadAllBytes(filePath);
            string fileName = Path.GetFileName(filePath);
            form.AddBinaryData(fieldName, fileData, fileName, mimeType);
        }
    }

    private string DetectMimeType(string path)
    {
        if (string.IsNullOrEmpty(path)) return "application/octet-stream";
        string ext = Path.GetExtension(path).ToLower();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }



    private IEnumerator FadeInPopup()
    {
        CanvasGroup cg = successPopupOverlay.GetComponent<CanvasGroup>();
        cg.alpha = 0;
        successPopupOverlay.SetActive(true);
        while (cg.alpha < 1f)
        {
            cg.alpha += Time.deltaTime * 2;
            yield return null;
        }
    }

    private IEnumerator FadeOutPopup()
    {
        CanvasGroup cg = successPopupOverlay.GetComponent<CanvasGroup>();
        while (cg.alpha > 0f)
        {
            cg.alpha -= Time.deltaTime * 2;
            yield return null;
        }
        successPopupOverlay.SetActive(false);
    }
}
