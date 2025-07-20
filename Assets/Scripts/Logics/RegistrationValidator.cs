// RegistrationValidator.cs
// Validates admin registration form fields, provides UI feedback, and manages state for account creation.
// Used in: Create a New Account, Request to Manage screens.
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

/// <summary>
/// Validates registration form inputs for admin creation and management requests.
/// Handles UI feedback, error highlighting, button state, and backend checks (e.g., email existence).
/// </summary>
public class RegistrationValidator : MonoBehaviour
{
    /* ──────────── CONSTANTS ──────────── */
    private static readonly Color COLOR_INVALID = new Color32(255, 211, 211, 130);
    private static readonly Color COLOR_VALID = new Color32(79, 140, 255, 70);
    private static readonly Color COLOR_ERROR = new Color32(241, 107, 107, 255);
    private const string CF_CHECK_EMAIL = "https://us-central1-smart-campus-navigation-105e9.cloudfunctions.net/checkEmailExists";

    /* ──────────── INSPECTOR FIELDS – unchanged ──────────── */
    [Header("Profile Picture")]
    [SerializeField] private Image profilePictureFrame;
    [SerializeField] private TMP_Text photoErrorText;

    [Header("Required Inputs")]
    [SerializeField] private TMP_InputField adminNameInput;
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField ensurePasswordInput;
    [SerializeField] private TMP_InputField roleInput;

    [Header("Optional Campus Field")]
    [SerializeField] public TMP_InputField campusNameInput;
    [SerializeField] public bool requireCampusName = false;

    [Header("File Upload")]
    [SerializeField] private FileUploadHandler approvalUploader;

    [Header("Error Labels")]
    [SerializeField] private TMP_Text adminNameError;
    [SerializeField] private TMP_Text emailError;
    [SerializeField] private TMP_Text passwordError;
    [SerializeField] private TMP_Text ensurePasswordError;
    [SerializeField] private TMP_Text roleError;
    [SerializeField] private TMP_Text approvalFileError;

    [Header("UI")]
    [SerializeField] private Button nextButton;

    /* ──────────── STATE – unchanged ──────────── */
    private bool profilePictureSelected;
    private bool approvalTouched;
    private bool emailFormatOK;
    private bool emailAvailable;
    private bool passwordOK;
    private bool ensurePasswordOK;
    private bool campusNameTouched;
    private Coroutine emailCheckRoutine;
    private string selectedProfileFileName;
    private Color photoTextDefaultColor;
    private string photoTextDefault;

    /* ──────────── UNITY ──────────── */
    private void Start()
    {
        /* default hint for photo label */
        L10n.Get("photo_hint", txt =>
        {               // e.g. "Attach profile photo"
            photoErrorText.text = txt;
            photoTextDefault = txt;
        });
        photoTextDefaultColor = photoErrorText.color;

        HideAllErrorLabels();
        AddListeners();
        RefreshUI();
    }

    /* ─────────────────────────────────────────────────────────────
     * LISTENER helpers – updated
     * ────────────────────────────────────────────────────────────*/
    private void AddListeners()
    {
        AddFieldListeners(adminNameInput, adminNameError, ValidateAdminName);
        AddFieldListeners(roleInput, roleError, ValidateRole);

        emailInput.onEndEdit.AddListener(OnEmailEndEdit);
        emailInput.onSelect.AddListener(_ => ClearError(emailError));
        emailInput.onValueChanged.AddListener(_ => { ClearError(emailError); ResetColor(emailInput); RefreshUI(); });

        passwordInput.onEndEdit.AddListener(_ => ValidatePassword(true));
        passwordInput.onValueChanged.AddListener(_ =>
        {
            ClearError(passwordError); ResetColor(passwordInput);
            ValidatePassword(); ValidateEnsurePassword(); RefreshUI();
        });

        ensurePasswordInput.onEndEdit.AddListener(_ => ValidateEnsurePassword(true));
        ensurePasswordInput.onValueChanged.AddListener(_ =>
        {
            ClearError(ensurePasswordError); ResetColor(ensurePasswordInput);
            ValidateEnsurePassword(); RefreshUI();
        });

        if (requireCampusName && campusNameInput != null)
        {
            campusNameInput.onSelect.AddListener(_ => { campusNameTouched = true; ResetColor(campusNameInput); });
            campusNameInput.onEndEdit.AddListener(_ => RefreshUI());
            campusNameInput.onValueChanged.AddListener(_ => RefreshUI());
        }

        // Listener for file selection
        if (approvalUploader != null)
        {
            approvalUploader.onValidationUpdate.AddListener(RefreshUI);
        }
    }

    private void AddFieldListeners(TMP_InputField field, TMP_Text label, System.Action validator)
    {
        field.onEndEdit.AddListener(_ => validator());
        field.onSelect.AddListener(_ => ClearError(label));
        field.onValueChanged.AddListener(_ => { ResetColor(field); ClearError(label); validator(); RefreshUI(); });
    }

    /* ─────────────────────────────────────────────────────────────
     * VALIDATORS  (localised)
     * ────────────────────────────────────────────────────────────*/
    private void ValidateAdminName()
    {
        bool ok = !string.IsNullOrWhiteSpace(adminNameInput.text);
        SetFieldStateLocalized(adminNameInput, adminNameError, ok, "err_name_required");
    }

    private void ValidateRole()
    {
        bool ok = !string.IsNullOrWhiteSpace(roleInput.text);
        SetFieldStateLocalized(roleInput, roleError, ok, "err_role_required");
    }

    private void ValidatePassword(bool showIfEmpty = false)
    {
        string p = passwordInput.text;
        passwordOK = false;

        if (string.IsNullOrWhiteSpace(p))
        {
            if (showIfEmpty)
                SetFieldStateLocalized(passwordInput, passwordError, false, "err_password_required");
            return;
        }

        if (p.Length < 6)
        {
            SetFieldStateLocalized(passwordInput, passwordError, false, "err_password_too_short");
            return;
        }

        SetFieldStateLocalized(passwordInput, passwordError, true);
        passwordOK = true;
    }

    private void ValidateEnsurePassword(bool showIfEmpty = false)
    {
        string confirm = ensurePasswordInput.text;
        ensurePasswordOK = false;

        if (string.IsNullOrWhiteSpace(confirm))
        {
            if (showIfEmpty)
                SetFieldStateLocalized(ensurePasswordInput, ensurePasswordError, false, "err_password_repeat");
            return;
        }

        if (confirm != passwordInput.text)
        {
            SetFieldStateLocalized(ensurePasswordInput, ensurePasswordError, false, "err_password_mismatch");
            return;
        }

        SetFieldStateLocalized(ensurePasswordInput, ensurePasswordError, true);
        ensurePasswordOK = true;
    }

    private void OnEmailEndEdit(string raw)
    {
        string email = raw.Trim();
        emailFormatOK = Regex.IsMatch(email, @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$");
        emailAvailable = false;

        if (!emailFormatOK)
        {
            SetFieldStateLocalized(emailInput, emailError, false,
                string.IsNullOrEmpty(email) ? "err_email_required" : "err_email_invalid");
            RefreshUI(); return;
        }

        if (emailCheckRoutine != null) StopCoroutine(emailCheckRoutine);
        emailCheckRoutine = StartCoroutine(CheckEmailCF(email));
    }

    private IEnumerator CheckEmailCF(string email)
    {
        using UnityWebRequest req = new(CF_CHECK_EMAIL, "POST");
        byte[] payload = System.Text.Encoding.UTF8.GetBytes($"{{\"email\":\"{email}\"}}");
        req.uploadHandler = new UploadHandlerRaw(payload);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success &&
                  !req.downloadHandler.text.Contains("\"exists\":true");

        emailAvailable = ok;
        SetFieldStateLocalized(emailInput, emailError, ok,
                               ok ? "" : "err_email_exists");

        RefreshUI();
    }

    /* ─────────────────────────────────────────────────────────────
       PHOTO LABEL  (localised) – updated logic
       ────────────────────────────────────────────────────────────*/
    private void UpdatePhotoLabel(bool allFieldsFilled)
    {
        if (profilePictureSelected)
        {
            photoErrorText.text = selectedProfileFileName;
            photoErrorText.color = photoTextDefaultColor;
        }
        else if (allFieldsFilled)
        {
            photoErrorText.color = COLOR_ERROR;
            L10n.Get("err_photo_required", txt =>
            {
                photoErrorText.text = txt;
            });
        }
        else
        {
            photoErrorText.text = photoTextDefault;
            photoErrorText.color = photoTextDefaultColor;
        }
    }

    /* ─────────────────────────────────────────────────────────────
       UI refresh & helpers – updated logic
       ────────────────────────────────────────────────────────────*/
    public void RefreshUI()
    {
        if (requireCampusName && campusNameInput != null && campusNameTouched)
        {
            campusNameInput.image.color =
                string.IsNullOrWhiteSpace(campusNameInput.text) ? COLOR_INVALID : COLOR_VALID;
        }

        approvalFileError.gameObject.SetActive(approvalTouched && !approvalUploader.validFileSelected);

        bool allFieldsFilled =
            !string.IsNullOrWhiteSpace(adminNameInput.text) &&
            emailFormatOK && emailAvailable &&
            passwordOK && ensurePasswordOK &&
            !string.IsNullOrWhiteSpace(roleInput.text) &&
            approvalUploader.validFileSelected;

        if (requireCampusName && campusNameInput != null)
            allFieldsFilled &= !string.IsNullOrWhiteSpace(campusNameInput.text);

        bool profilePictureFilled = profilePictureSelected;

        UpdatePhotoLabel(allFieldsFilled);

        nextButton.interactable = allFieldsFilled && profilePictureFilled;
    }

    private bool IsFormValid()
    {
        bool ok =
            !string.IsNullOrWhiteSpace(adminNameInput.text) &&
            emailFormatOK && emailAvailable &&
            passwordOK && ensurePasswordOK &&
            !string.IsNullOrWhiteSpace(roleInput.text) &&
            approvalUploader.validFileSelected &&
            profilePictureSelected;

        if (requireCampusName)
            ok &= !string.IsNullOrWhiteSpace(campusNameInput.text);

        return ok;
    }

    /* ─────────────────────────────────────────────────────────────
       GENERIC error helpers  (localised)
       ────────────────────────────────────────────────────────────*/
    private void SetFieldStateLocalized(TMP_InputField field, TMP_Text label, bool valid, string key = "")
    {
        if (valid)
        {
            field.image.color = COLOR_VALID;
            label.gameObject.SetActive(false);
        }
        else
        {
            field.image.color = COLOR_INVALID;

            if (!string.IsNullOrWhiteSpace(key))
            {
                L10n.Get(key, txt =>
                {
                    string col = ColorUtility.ToHtmlStringRGB(COLOR_ERROR);
                    label.text = $"<color=#{col}>{txt}</color>";
                    label.gameObject.SetActive(true);
                });
            }
        }
    }

    private void HideAllErrorLabels()
    {
        adminNameError.gameObject.SetActive(false);
        emailError.gameObject.SetActive(false);
        passwordError.gameObject.SetActive(false);
        ensurePasswordError.gameObject.SetActive(false);
        roleError.gameObject.SetActive(false);
        approvalFileError.gameObject.SetActive(false);
    }
    private static void ResetColor(TMP_InputField f) { if (f?.image != null) f.image.color = Color.white; }
    private void ClearError(TMP_Text l) => l?.gameObject.SetActive(false);

    public void DisplayProfilePicture(string path)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(path));
        profilePictureFrame.sprite = Sprite.Create(
            tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        selectedProfileFileName = Path.GetFileName(path);
        profilePictureSelected = true;
        profilePictureFrame.type = Image.Type.Simple;
        profilePictureFrame.preserveAspect = false;
        RefreshUI();
    }

}
