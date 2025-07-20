using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles display of all login feedback messages to the user.
/// Supports localized messages with visual styling for error, info, and success.
/// Used in: Login screen.
/// </summary>
public class LoginUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Image messagePanel;

    public void ShowMessage(string key, string type)
    {
        L10n.Get(key, text => messageText.text = text);
        messageText.gameObject.SetActive(true);

        switch (type)
        {
            case "error":
                messageText.color = new Color32(198, 40, 40, 255);
                if (messagePanel != null) messagePanel.color = new Color(1f, 0f, 0f, 0.05f);
                break;
            case "info":
                messageText.color = new Color32(21, 101, 192, 255);
                if (messagePanel != null) messagePanel.color = new Color(0.3f, 0.6f, 1f, 0.05f);
                break;
            case "success":
                messageText.color = new Color32(46, 125, 50, 255);
                if (messagePanel != null) messagePanel.color = new Color(0.2f, 1f, 0.2f, 0.08f);
                break;
        }
    }

    public void HideMessage()
    {
        messageText.text = "";
        messageText.gameObject.SetActive(false);
        if (messagePanel != null) messagePanel.color = Color.clear;
    }

    public void ShowWrongPassword()
    {
        ShowMessage("err_wrong_password", "error");
    }

    public void ShowWaitingApproval()
    {
        ShowMessage("info_waiting_approval", "info");
    }

    public void ShowRejected()
    {
        ShowMessage("err_registration_rejected", "error");
    }

    public void ShowUserNotFound()
    {
        ShowMessage("err_user_not_found", "error");
    }

    public void ShowLoginSuccess(string adminName)
    {
        L10n.Get("msg_login_success", text => messageText.text = text);
        messageText.text = messageText.text + " " + adminName; // Append dynamic name if needed
        messageText.gameObject.SetActive(true);
        messageText.color = new Color32(46, 125, 50, 255);
        if (messagePanel != null) messagePanel.color = new Color(0.2f, 1f, 0.2f, 0.08f);


    }
}

/*
    --- Key Concepts ---

    - Localized Feedback: Uses localization keys for multilingual support.
    - Visual Clarity: Message type controls color and panel styling for user clarity.
    - Tight Integration: Used by LoginManager for all login results (error, info, success).
    - User Experience: Ensures user always knows the login status and any issues.
*/