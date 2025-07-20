using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.Events;

#if UNITY_ANDROID || UNITY_IOS
using NativeFilePickerNamespace;
using NativeCameraNamespace;
#endif
#if UNITY_STANDALONE || UNITY_EDITOR
using SFB; // StandaloneFileBrowser for desktop file picking
#endif

/// <summary>
/// Manages file selection and validation for file uploads in forms.
/// Supports picking from file system, gallery, or camera (desktop and mobile).
/// Validates file extension, size, and existence, and updates UI and listeners.
/// </summary>
public class FileUploadHandler : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text fileNameText;

    [Header("Behavior")]
    public bool showFileName = true;
    public string[] acceptedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

    [Header("Callback")]
    public UnityEvent<string> onFileSelected;

    [Header("Validator Notifier")]
    public UnityEvent onValidationUpdate;

    [HideInInspector]
    public bool validFileSelected = false;

    [Header("Action Sheet")]
    public ActionSheetController actionSheet;

    // Call this from your upload button
    public void OpenFileSheet()
    {
        actionSheet.OnFile = OpenFilePicker;
        actionSheet.OnGallery = OpenGalleryPicker;
        actionSheet.OnCamera = OpenCameraPicker;
        actionSheet.Show();
    }

    // --- Action handlers for each option ---

    // File picker: any file type
    private void OpenFilePicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeFilePicker.PickFile((path) =>
        {
            ValidateAndShowFile(path);
        }, null, "Select file");
#else
        var paths = StandaloneFileBrowser.OpenFilePanel("Select file", "", "", false);
        string path = (paths.Length > 0) ? paths[0] : null;
        ValidateAndShowFile(path);
#endif
    }

    // Gallery picker: only images
    private void OpenGalleryPicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeFilePicker.PickFile((path) =>
        {
            ValidateAndShowFile(path);
        }, "image/*", "Select image from gallery");
#else
        var paths = StandaloneFileBrowser.OpenFilePanel("Select image", "", "jpg,png,jpeg", false);
        string path = (paths.Length > 0) ? paths[0] : null;
        ValidateAndShowFile(path);
#endif
    }

    private void OpenCameraPicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeCamera.TakePicture((path) =>
        {
            ValidateAndShowFile(path);
        }, 1024); // Optional max size
#else
        var paths = StandaloneFileBrowser.OpenFilePanel("Take photo (Select image)", "", "jpg,png,jpeg", false);
        string path = (paths.Length > 0) ? paths[0] : null;
        ValidateAndShowFile(path);
#endif
    }





    // --- File validation and UI update ---
    private void ValidateAndShowFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            SetFileResult("No file chosen", false);
            return;
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        FileInfo fileInfo = new FileInfo(path);
        if (!fileInfo.Exists || fileInfo.Length == 0)
        {
            SetFileResult("File is empty or missing", false);
            return;
        }

        if (acceptedExtensions.Length > 0 && !acceptedExtensions.Contains(ext))
        {
            SetFileResult("Invalid file format", false);
            return;
        }

        SetFileResult("Selected File: " + Path.GetFileName(path), true);
        onFileSelected?.Invoke(path);
    }

    // Helper: set UI and state
    private void SetFileResult(string message, bool valid)
    {
        if (showFileName && fileNameText != null)
            fileNameText.text = message;
        validFileSelected = valid;
        onValidationUpdate?.Invoke();
    }
}

/*
    --- Key Concepts ---

    - acceptedExtensions: Only allows file types listed (default: PDF, JPEG, PNG). Can be set per instance.
    - Platform Support: Uses native pickers for Android/iOS, StandaloneFileBrowser for desktop.
    - onFileSelected: Invoked when a valid file is picked, passing the file path to listeners (e.g., parent forms).
    - onValidationUpdate: Notifies parent validators/UI after each selection or validation.

    --- Logic Summary ---

    - Presents a choice to the user: pick a file, select from gallery, or take a new photo.
    - Validates the file type, existence, and size.
    - Updates the UI and validation state for parent forms to act accordingly.
*/