using UnityEngine;
using TMPro;

public class KeyboardSmartShift : MonoBehaviour
{
    public RectTransform panelToMove;      // main container
    public float padding = 100f;           // extra clearance
    public float epsilon = 2f;             // tolerance in px (ignore < 2px)

#if UNITY_EDITOR
    public bool simulateKeyboard = false;
    public float simulatedKeyboardHeight = 600f;
#endif

    private Vector2 originalPos;
    private TMP_InputField currentInput;
    private bool panelShifted = false;     // have we shifted already?
    private float lastShift = 0f;          // remember applied shift

    /* ------------------------------------------------------------ */
    void Start()
    {
        originalPos = panelToMove.anchoredPosition;

        foreach (var inp in panelToMove.GetComponentsInChildren<TMP_InputField>(true))
        {
            inp.onSelect.AddListener(_ => currentInput = inp);
            inp.onDeselect.AddListener(_ => currentInput = null);
        }
    }

    /* ------------------------------------------------------------ */
    void Update()
    {
        bool kbVisible = TouchScreenKeyboard.visible;
#if UNITY_EDITOR
        kbVisible |= simulateKeyboard;
#endif

        if (kbVisible && currentInput != null)
            TryShiftUp();
        else if (panelShifted)
            RestorePanel();                // keyboard closed / input lost
    }

    /* ------------------------------------------------------------ */
    private void TryShiftUp()
    {
        // get input bottom Y on screen
        Vector3[] corners = new Vector3[4];
        currentInput.GetComponent<RectTransform>().GetWorldCorners(corners);
        float inputBottomY = corners[0].y;

#if UNITY_EDITOR
        float keyboardY = simulateKeyboard ? simulatedKeyboardHeight : TouchScreenKeyboard.area.y;
#else
        float keyboardY = TouchScreenKeyboard.area.y;
#endif

        float shift = (keyboardY + padding) - inputBottomY;   // positive -> need to move up

        // if field is covered enough (beyond epsilon) AND we aren t already shifted to ~that amount
        if (shift > epsilon && Mathf.Abs(shift - lastShift) > epsilon)
        {
            panelToMove.anchoredPosition = originalPos + new Vector2(0f, shift);
            lastShift = shift;
            panelShifted = true;
        }
    }

    /* ------------------------------------------------------------ */
    private void RestorePanel()
    {
        panelToMove.anchoredPosition = originalPos;
        panelShifted = false;
        lastShift = 0f;
    }
}
