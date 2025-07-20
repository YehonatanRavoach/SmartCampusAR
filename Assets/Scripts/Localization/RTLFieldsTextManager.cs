using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;
using UnityEngine.UI;   // ⭢ for LayoutGroup

/******************************************************************************
 * RTLFieldsTextManager.cs
 * -----------------------------------------------------------------------------
 * Utility that flips UI alignment when the current locale is Hebrew (“he”).
 *
 *  How it works
 *  ------------
 *  • On start and whenever `SelectedLocale` changes the script calls
 *    <see cref="UpdateRTL"/>.  
 *  • If the selected locale’s code is **"he"** it treats the UI as RTL,
 *    otherwise as LTR.  
 *
 *  Three categories are handled:
 *  --------------------------------
 *   ① **rtlTexts**   Any `TMP_Text` that only needs its
 *      `isRightToLeftText` flag and horizontal alignment changed.  
 *
 *   ② **flipLayoutGroups** `LayoutGroup`s (Vertical/Horizontal) whose
 *      `childAlignment` must mirror between left and right edges.  
 *
 *   ③ **flipRects**  Single `RectTransform`s that should jump to the
 *      opposite side of their parent (icons, buttons, input fields, etc.).  
 *
 *  Customisation
 *  -------------
 *  • Drag any number of objects into the three Inspector arrays.  
 *  • Tweak `EDGE_PADDING` in <see cref="FlipRectTransform"/> for the gap you
 *    want between a flipped element and the screen edge.  
 *****************************************************************************/

public class RTLFieldsTextManager : MonoBehaviour
{
    /* ①  Text blocks that only need RTL/LTR alignment */
    [Header("Text components that flip direction only")]
    public TMP_Text[] rtlTexts;

    /* ②  LayoutGroups whose CHILDREN should align left←→right      *
     *    (Vertical or Horizontal Layout Group)                     */
    [Header("LayoutGroups that must realign")]
    public LayoutGroup[] flipLayoutGroups;

    /* ③  RectTransforms (single items) that must jump to the       *
     *    opposite edge (e.g. an icon, a button, an input field)    */
    [Header("Individual RectTransforms to mirror")]
    public RectTransform[] flipRects;

    /* ----------------------------------------------------------- */

    void Awake()
    {                       // apply once on app launch
        UpdateRTL();
    }

    void OnEnable()         // keep syncing when language changes
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(UnityEngine.Localization.Locale _) => UpdateRTL();

    /* ----------------------------------------------------------- */

    public void UpdateRTL()
    {
        bool isHebrew =
            LocalizationSettings.SelectedLocale?.Identifier.Code == "he";

        /* 1️⃣   TEXT DIRECTION + ALIGNMENT ---------------------- */
        foreach (var txt in rtlTexts)
        {
            if (!txt) continue;
            txt.isRightToLeftText = isHebrew;
            txt.alignment = isHebrew
                ? TextAlignmentOptions.Right
                : TextAlignmentOptions.Left;
        }

        /* 2️⃣   LAYOUT GROUPS  ---------------------------------- */
        foreach (var lg in flipLayoutGroups)
        {
            if (!lg) continue;
            lg.childAlignment = isHebrew
                ? TextAnchor.UpperRight   // or MiddleRight / LowerRight
                : TextAnchor.UpperLeft;   // mirror for LTR
        }

        /* 3️⃣   SOLO ELEMENTS  ---------------------------------- */
        foreach (var rt in flipRects)
        {
            if (!rt) continue;
            FlipRectTransform(rt, isHebrew);
        }
    }

    /* Helper: mirror a single RectTransform horizontally */
    void FlipRectTransform(RectTransform rt, bool toRTL)
    {
        //  anchor & pivot to right side if RTL, left if LTR
        float xAnchor = toRTL ? 1f : 0f;
        rt.anchorMin = new Vector2(xAnchor, rt.anchorMin.y);
        rt.anchorMax = new Vector2(xAnchor, rt.anchorMax.y);
        rt.pivot = new Vector2(xAnchor, rt.pivot.y);

        // flush to the edge, plus optional padding
        const float EDGE_PADDING = 26f;            // change to 8, 12… if you want a gap
        rt.anchoredPosition = new Vector2(toRTL ? -EDGE_PADDING : EDGE_PADDING, rt.anchoredPosition.y);
    }
}