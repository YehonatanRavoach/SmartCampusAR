/***************************************************************
 * SmartInputDirection.cs
 * Attach to any TMP_InputField. Handles RTL/LTR automatically
 * and injects LRM/RLM for mixed Hebrew–English sentences.
 ***************************************************************/
using UnityEngine;
using TMPro;

/// <summary>
/// Auto-detect base direction (Hebrew → RTL, else LTR) and optionally.
/// </summary>
public class AutoTextDirection : MonoBehaviour
{
    [Header("Alignment")]
    public TextAlignmentOptions ltrAlignment = TextAlignmentOptions.Left;
    public TextAlignmentOptions rtlAlignment = TextAlignmentOptions.Right;

    [Header("Behaviour")]
    [Tooltip("Inject Unicode LRM/RLM to fix mixed RTL/LTR segments.")]
    public bool useDirectionalMarks = false;

    private TMP_InputField input;
    private TMP_Text       textComp;
    private string         lastRaw = "";

    /* ----------------------- Unity ----------------------- */
    void Awake()
    {
        input    = GetComponent<TMP_InputField>();
        textComp = input.textComponent;

        input.onValueChanged.AddListener(OnTextChanged);
    }
    void OnDestroy() => input.onValueChanged.RemoveListener(OnTextChanged);

    /* ---------------- value changed ---------------------- */
    private void OnTextChanged(string raw)
    {
        if (raw == lastRaw) return;          // no change
        lastRaw = raw;

        
        bool rtlBase = FirstStrongIsHebrew(raw);
        textComp.isRightToLeftText = rtlBase;
        textComp.alignment         = rtlBase ? rtlAlignment : ltrAlignment;

        
        if (useDirectionalMarks)
        {
            string fixedText = rtlBase
                              ? InjectLRM(raw)  
                              : InjectRLM(raw);  
            // set without invoking callback again
            input.SetTextWithoutNotify(fixedText);
        }
    }

    /* ---------------- Helpers ---------------------------- */
    /// Detect: first strong char – Hebrew?
    private bool FirstStrongIsHebrew(string s)
    {
        foreach (char c in s)
        {
            if (char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsDigit(c))
                continue;

            if (IsHebrew(c)) return true;          // RTL
            if (IsLatin(c))  return false;         // LTR
            // other scripts → treat as LTR by default
        }
        return false;
    }

    /* Inject Left-To-Right Mark (LRM) around latin runs in RTL base */
    private string InjectLRM(string src)
    {
        const char LRM = '\u200E';
        System.Text.StringBuilder sb = new();
        bool inLatin = false;

        foreach (char c in src)
        {
            bool latin = IsLatin(c);
            if (latin && !inLatin) { sb.Append(LRM); inLatin = true; }
            if (!latin && inLatin) { sb.Append(LRM); inLatin = false; }

            sb.Append(c);
        }
        if (inLatin) sb.Append(LRM);
        return sb.ToString();
    }

    /* Inject Right-To-Left Mark (RLM) around hebrew runs in LTR base */
    private string InjectRLM(string src)
    {
        const char RLM = '\u200F';
        System.Text.StringBuilder sb = new();
        bool inHebrew = false;

        foreach (char c in src)
        {
            bool heb = IsHebrew(c);
            if (heb && !inHebrew) { sb.Append(RLM); inHebrew = true; }
            if (!heb && inHebrew) { sb.Append(RLM); inHebrew = false; }

            sb.Append(c);
        }
        if (inHebrew) sb.Append(RLM);
        return sb.ToString();
    }

    /* Basic char tests */
    private bool IsHebrew(char c) => (c >= 0x0590 && c <= 0x05FF);
    private bool IsLatin (char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
}
