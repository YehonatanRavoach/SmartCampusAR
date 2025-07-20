using UnityEngine;
using TMPro;
/******************************************************************************
 * TextDirectionFixer.cs
 * -----------------------------------------------------------------------------
 * Tiny utility component that inspects a TextMeshProUGUI’s current content and
 * chooses the correct text‑direction & alignment—either at runtime or
 * continuously every frame.
 *
 *  Why?
 *  ----
 *  • Mixed Hebrew + English interfaces often need some blocks LEFT‑aligned
 *    (English, numbers) and some RIGHT‑aligned (Hebrew).  
 *  • Unity / TextMeshPro can flip glyph order when you set
 *    `isRightToLeftText = true`, but you still need to choose alignment
 *    (Left vs Right) and sometimes enforce a fixed mode regardless of the
 *    text.  
 *
 *  How it works
 *  ------------
 *  • In **AutoByLanguage** it simply checks whether the string contains any
 *    Unicode code‑points in the Hebrew block (0x0590‑0x05FF).  
 *  • If so → applies `rtlAlignment` (default = Right) and sets
 *    `isRightToLeftText = true`; otherwise uses `ltrAlignment`.  
 *  • Other modes override auto‑detection:
 *        – AlwaysCenter   → align centre, always LTR  
 *        – AlwaysLTR      → fixed `ltrAlignment`  
 *        – AlwaysRTL      → fixed `rtlAlignment`  
 *
 *  Usage
 *  -----
 *      1. Add the script to a TextMeshProUGUI object (auto‑added by
 *         `[RequireComponent]`).  
 *      2. Pick a **Mode** or leave it on *AutoByLanguage*.  
 *      3. If you need the component to react to text that changes every
 *         Update—for example, a chat message—enable **checkEveryFrame**.  
 *      4. When you set text via code, you can call
 *         `myFixer.ApplyDirection(newText)` manually for immediate refresh.  
 *
 *  Fields
 *  ------
 *      alignmentMode     — Behaviour mode (enum above).  
 *      checkEveryFrame   — Perform detection every Update (costly if many).  
 *      ltrAlignment      — Alignment preset when rendering LTR.  
 *      rtlAlignment      — Alignment preset when rendering RTL.  
 *****************************************************************************/
[RequireComponent(typeof(TextMeshProUGUI))]
public class TextDirectionFixer : MonoBehaviour
{
    public enum Mode
    {
        AutoByLanguage,   
        AlwaysCenter,     
        AlwaysLTR,        
        AlwaysRTL         
    }

    [Header("Behaviour")]
    public Mode alignmentMode = Mode.AutoByLanguage;

    [Tooltip("Check every frame for changes (needed for texts that update a lot).")]
    public bool checkEveryFrame = false;

    [Header("Auto mode ‒ alignment presets")]
    public TextAlignmentOptions ltrAlignment = TextAlignmentOptions.Left;
    public TextAlignmentOptions rtlAlignment = TextAlignmentOptions.Right;

    /* ───────────── private ───────────── */
    private TextMeshProUGUI tmp;
    private string lastText = "";

    void Awake() => tmp = GetComponent<TextMeshProUGUI>();

    void Start() => ApplyDirection(tmp.text, true);

    void Update()
    {
        if (!checkEveryFrame) return;

        if (tmp.text != lastText)
            ApplyDirection(tmp.text);
    }

    /// <summary>Call manually if you set text from code.</summary>
    public void ApplyDirection(string text, bool force = false)
    {
        if (string.IsNullOrWhiteSpace(text) || tmp == null) return;
        if (!force && text == lastText) return;

        switch (alignmentMode)
        {
            case Mode.AlwaysCenter:
                tmp.alignment         = TextAlignmentOptions.Center;
                tmp.isRightToLeftText = false;
                break;

            case Mode.AlwaysLTR:
                tmp.alignment         = ltrAlignment;
                tmp.isRightToLeftText = false;
                break;

            case Mode.AlwaysRTL:
                tmp.alignment         = rtlAlignment;
                tmp.isRightToLeftText = true;
                break;

            case Mode.AutoByLanguage:
            default:
                bool rtl = ContainsHebrew(text);
                tmp.alignment         = rtl ? rtlAlignment : ltrAlignment;
                tmp.isRightToLeftText = rtl;
                break;
        }

        tmp.raycastTarget = false;   
        lastText          = text;
    }

    private bool ContainsHebrew(string s)
    {
        foreach (char c in s)
            if (c >= 0x0590 && c <= 0x05FF) return true;
        return false;
    }
}
