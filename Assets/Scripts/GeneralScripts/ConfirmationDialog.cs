using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
/*
 * ConfirmationDialog.cs
 * -----------------------------------------------------------------------------
 * A simple two-button (“Yes / No”) modal dialog.
 *
 *  • Call <see cref="Initialize"/> immediately after instantiation to inject
 *    the message text and the callbacks for each button.  
 *  • The dialog destroys its own GameObject once either button is pressed, so
 *    callers don’t need to clean it up explicitly.  
 *
 
 */
 
public class ConfirmationDialog : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public Button yesButton;
    public Button noButton;
 
    private Action onYes;
    private Action onNo;
    
    // ------------------------- Initialize -------------------------
    /// <summary> 
    /// Initializes the dialog with a message and button callbacks.
    /// </summary>
    /// <param name="message">The message to display in the dialog.</param>
    /// <param name="yesCallback">Callback for the Yes button.</param>
    /// <param name="noCallback">Optional callback for the No button.</param>
  
    
 
    public void Initialize(string message, Action yesCallback, Action noCallback = null)
    {
        messageText.text = message;
        onYes = yesCallback;
        onNo = noCallback;

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(() =>
        {
            onYes?.Invoke();
            CloseDialog();
        });

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(() =>
        {
            onNo?.Invoke();
            CloseDialog();
        });
    }
 
    private void CloseDialog()
    {
        Destroy(gameObject);
    }
}