// Assets/Scripts/Localization/L10n.cs
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
/*
 * L10n.cs
 * -----------------------------------------------------------------------------
 * Tiny convenience wrapper around Unity‑Localization’s `LocalizedString`
 * that makes it easier to:
 *
 *   • **Set a TextMeshPro component** directly from a String‑Table key
 *     — see <see cref="SetText"/>.  
 *   • **Fetch a localized string** asynchronously and use it in your own
 *     code (dialogs, logs, etc.) — see <see cref="Get"/>.  
 *
 *  Behaviour
 *  ---------
 *  • Both methods target the **"ErrorMessages"** String‑Table Collection
 *    (change the first argument of `LocalizedString` if you need another
 *    table).  
 *  • If the lookup fails for any reason, they fall back to **displaying the
 *    key itself** — handy during development when translations may be
 *    missing.  
 *  • They attach a completion‑callback to the `AsyncOperationHandle<string>`
 *    returned by `GetLocalizedStringAsync()`. No need to store the handle;
 *    memory is released automatically when the operation completes.  
 *
 *  Usage
 *  -----
 *      // 1. One‑liner for UI
 *      L10n.SetText(myTmpText, "network_error");
 *
 *      // 2. Retrieve first, then use
 *      L10n.Get("confirm_delete", localized =>
 *      {
 *          ShowDialog(localized);
 *      });
 *
 */

public static class L10n
{
    public static void SetText(TMP_Text target, string key)// Set a TextMeshPro component's text
    {
        if (target == null) return;
        var ls = new LocalizedString("ErrorMessages", key);
        AsyncOperationHandle<string> h = ls.GetLocalizedStringAsync();
        h.Completed += op =>
        {
            target.text = op.Status == AsyncOperationStatus.Succeeded
                        ? op.Result
                        : key;                 // dev fallback
        };
    }
    public static void Get(string key, System.Action<string> onReady)// Fetch a localized string asynchronously
    {
        var ls = new LocalizedString("ErrorMessages", key);
    
        var handle = ls.GetLocalizedStringAsync();
        handle.Completed += op =>
        {
            string result = op.Status == AsyncOperationStatus.Succeeded
                            ? op.Result
                            : key;              // dev fallback
            onReady?.Invoke(result);
        };
    }
}