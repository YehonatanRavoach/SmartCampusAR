using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyboardAwareScroll : MonoBehaviour
{
    public ScrollRect scrollRect;
    public RectTransform content;

    private TMP_InputField currentInput;

    void Start()
    {
        var inputs = content.GetComponentsInChildren<TMP_InputField>(true);
        foreach (var input in inputs)
        {
            input.onSelect.AddListener(delegate { OnInputSelected(input); });
        }
    }

    void Update()
    {
        if (TouchScreenKeyboard.visible && currentInput != null)
        {
            Rect keyboardRect = TouchScreenKeyboard.area;
            Vector3[] corners = new Vector3[4];
            currentInput.GetComponent<RectTransform>().GetWorldCorners(corners);

            float inputBottom = corners[0].y;

            if (inputBottom < keyboardRect.y + 100f)
            {
                StartCoroutine(ScrollToInput(currentInput));
            }
        }
    }

    private void OnInputSelected(TMP_InputField input)
    {
        currentInput = input;
    }

    private IEnumerator ScrollToInput(TMP_InputField input)
    {
        yield return new WaitForEndOfFrame();

        RectTransform inputRect = input.GetComponent<RectTransform>();
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, inputRect.position, null, out localPos);

        float normalizedPos = Mathf.Clamp01(1 - (localPos.y / content.rect.height));
        scrollRect.verticalNormalizedPosition = normalizedPos;
    }
}
