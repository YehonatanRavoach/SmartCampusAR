using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Validates all campus detail fields in the campus registration form.
/// Checks required text, file uploads, and dropdown selections.
/// Provides UI feedback, manages the submit button, and checks campus name uniqueness via backend.
/// Used in: Create a New Account (Step 2: Campus Details)
/// </summary>
public class CampusValidator : MonoBehaviour
{
    [Header("Required Inputs")]
    public TMP_InputField campusNameInput;
    public TMP_InputField descriptionInput;

    [Header("File Uploaders")]
    public FileUploadHandler logoUploader;
    public FileUploadHandler mapUploader;

    [Header("Error Messages")]
    public TMP_Text campusNameError;
    public TMP_Text descriptionError;
    public TMP_Text countryError;
    public TMP_Text cityError;
    public TMP_Text logoFileError;
    public TMP_Text mapFileError;

    [Header("UI References")]
    public Button submitButton;

    [Header("Dropdown Controller")]
    public CountryCityDropdownController dropdownController;

    // Color logic
    private Color validColor = new Color32(79, 140, 255, 70);    // Soft blue
    private Color invalidColor = new Color32(255, 211, 211, 130); // Soft red
    private Color defaultColor = Color.white;

    // Cached selected values (for validation)
    private string selectedCountry = "";
    private string selectedCity = "";
    private bool logoTouched = false;
    private bool mapTouched = false;


    private const string checkCampusExistsUrl = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/checkCampusExists";

    private void Start()
    {
        AddInputListeners();
        UpdateSubmitButton();

        dropdownController.OnCountrySelected += country =>
        {
            selectedCountry = country;
            countryError.gameObject.SetActive(false);
            UpdateSubmitButton();
        };

        dropdownController.OnCitySelected += city =>
        {
            selectedCity = city;
            cityError.gameObject.SetActive(false);
            UpdateSubmitButton();
        };
    }

    private void AddInputListeners()
    {
        AddFieldValidation(campusNameInput, campusNameError, ValidateCampusNameAsync);
        AddFieldValidation(descriptionInput, descriptionError, ValidateDescription);
    }

    private void AddFieldValidation(TMP_InputField input, TMP_Text errorText, System.Action validator)
    {
        input.onEndEdit.AddListener(_ => validator());

        input.onValueChanged.AddListener(value =>
        {
            if (!string.IsNullOrWhiteSpace(value))
                errorText.gameObject.SetActive(false);

            // Reset color to default when typing
            if (input != null && input.image != null)
                input.image.color = defaultColor;

            UpdateSubmitButton();
        });

        input.onSelect.AddListener(_ => {
            errorText.gameObject.SetActive(false);
            if (input != null && input.image != null)
                input.image.color = defaultColor;
        });
    }

    private void UpdateSubmitButton()
    {
        submitButton.interactable = IsFormValid();

        logoFileError.gameObject.SetActive(logoTouched && !logoUploader.validFileSelected);
        mapFileError.gameObject.SetActive(mapTouched && !mapUploader.validFileSelected);
    }

    private bool IsFormValid()
    {
        return
            !string.IsNullOrWhiteSpace(campusNameInput.text) &&
            !string.IsNullOrWhiteSpace(descriptionInput.text) &&
            !string.IsNullOrWhiteSpace(selectedCountry) &&
            !string.IsNullOrWhiteSpace(selectedCity) &&
            logoUploader.validFileSelected &&
            mapUploader.validFileSelected;
    }

    // --- CAMPUS NAME VALIDATION ---
    private void ValidateCampusNameAsync()
    {
        string name = campusNameInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            SetFieldInvalid(campusNameInput, campusNameError, "err_campus_name_required");

            return;
        }

        if (name.Length < 3 || name.Length > 20)
        {
           SetFieldInvalid(campusNameInput, campusNameError, "err_campus_name_length");

            return;
        }

        StartCoroutine(CheckCampusNameExistsViaCF(name));
    }



    private IEnumerator CheckCampusNameExistsViaCF(string name)
    {
        string postData = "{\"name\":\"" + name + "\"}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);

        using (UnityWebRequest req = new UnityWebRequest(checkCampusExistsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to check campus: " + req.error);
                SetFieldInvalid(campusNameInput, campusNameError, "err_campus_name_verify_failed");

                yield break;
            }

            string json = req.downloadHandler.text;

            if (json.Contains("\"exists\":true"))
            {
                SetFieldInvalid(campusNameInput, campusNameError, "err_campus_name_exists");

            }
            else
            {
                SetFieldValid(campusNameInput, campusNameError);
            }
        }
    }




    // --- DESCRIPTION VALIDATION ---
    private void ValidateDescription()
    {
        string text = descriptionInput.text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            SetFieldInvalid(descriptionInput, descriptionError, "err_description_required");

            return;
        }
        if (text.Length < 10)
        {
            SetFieldInvalid(descriptionInput, descriptionError, "err_description_too_short");

            return;
        }
        SetFieldValid(descriptionInput, descriptionError);
    }

    // File upload triggers
    public void MarkLogoTouched()
    {
        logoTouched = true;
        UpdateSubmitButton();
    }

    public void MarkMapTouched()
    {
        mapTouched = true;
        UpdateSubmitButton();
    }

    public string GetSelectedCountry() => selectedCountry;
    public string GetSelectedCity() => selectedCity;

    // --- Helper: Set field valid/invalid coloring ---
    private void SetFieldValid(TMP_InputField input, TMP_Text errorText)
    {
        if (input != null && input.image != null)
            input.image.color = validColor;
        if (errorText != null)
            errorText.gameObject.SetActive(false);
    }

private void SetFieldInvalid(TMP_InputField input, TMP_Text errorText, string localizationKey)
{
    if (input != null && input.image != null)
        input.image.color = invalidColor;

    
        L10n.Get(localizationKey, localizedMsg =>
        {
            errorText.text = localizedMsg;
            errorText.gameObject.SetActive(true);
        });
    
}
}
