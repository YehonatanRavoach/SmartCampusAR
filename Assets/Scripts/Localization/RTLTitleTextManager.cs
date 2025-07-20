using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;
/******************************************************************************
 * RTLTitleTextManager.cs
 * -----------------------------------------------------------------------------
 * Keeps title / headline `TMP_Text` components centred while toggling
 * right‑to‑left rendering when the active locale is Hebrew.
 *
 *  Behaviour
 *  ---------
 *  • On Awake() it immediately aligns text according to the launch locale.  
 *  • Subscribes to `LocalizationSettings.SelectedLocaleChanged` so that titles
 *    flip direction instantly whenever the player switches language.  
 *  • For titles we always keep **centre alignment** (`TextAlignmentOptions.Center`);
 *    the script only flips `isRightToLeftText` so glyph order is correct.  
 *
 *  Usage
 *  -----
 *    • Drag any number of headline `TMP_Text` objects into **rtlTexts**.  
 *    • Add the component to a convenient GameObject (one instance is enough
 *      per scene).  
 *****************************************************************************/
 
public class RTLTitleTextManager : MonoBehaviour
{
    public TMP_Text[] rtlTexts;
    void Awake()
    {
        UpdateRTL(); // Apply once on app launch
    }
 
    void OnEnable() // Keep syncing when language changes
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }
 
    void OnDisable()// Unsubscribe to avoid memory leaks
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }
 
    void OnLocaleChanged(UnityEngine.Localization.Locale obj)// When the locale changes
    {
        UpdateRTL();
    }
 
    public void UpdateRTL()// Update all text components
    {
        var selectedLocale = LocalizationSettings.SelectedLocale;
        bool isHebrew = selectedLocale != null && selectedLocale.Identifier.Code == "he";
        foreach (var txt in rtlTexts)
        {
            if (txt == null) continue;
            txt.isRightToLeftText = isHebrew;
            txt.alignment = isHebrew ? TextAlignmentOptions.Center : TextAlignmentOptions.Center;
        }
    }
}

