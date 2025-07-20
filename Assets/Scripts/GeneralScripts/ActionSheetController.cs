using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ActionSheetController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panelBackground;       // The semi-transparent overlay (should have Button + Image components)
    public RectTransform container;          // The white rounded popup (bottom panel)
    public Button fileBtn, galleryBtn, cameraBtn, cancelBtn;

    // Callbacks for each action
    public System.Action OnFile, OnGallery, OnCamera;

    [Header("Animation")]
    public float animationTime = 0.22f;
    private Vector2 containerHiddenPos;

    private void Awake()
    {
        // Cache the original (shown) position
        containerHiddenPos = new Vector2(0, -container.rect.height);

        // Hook up button listeners
        fileBtn.onClick.AddListener(() => { Hide(); OnFile?.Invoke(); });
        galleryBtn.onClick.AddListener(() => { Hide(); OnGallery?.Invoke(); });
        cameraBtn.onClick.AddListener(() => { Hide(); OnCamera?.Invoke(); });
        cancelBtn.onClick.AddListener(Hide);

        // Hide on background click (must have Button component)
        if (panelBackground.GetComponent<Button>() != null)
            panelBackground.GetComponent<Button>().onClick.AddListener(Hide);

        HideInstant();
    }

    public void Show()
    {
        panelBackground.SetActive(true);
        container.gameObject.SetActive(true);

        float height = container.rect.height;
        if (height < 2f)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            height = container.rect.height;
        }

        float yOffset = 200f; // The vertical distance from the bottom
        containerHiddenPos = new Vector2(0, -height + yOffset);

        container.anchoredPosition = containerHiddenPos;
        container.DOAnchorPos(new Vector2(0, yOffset), animationTime).SetEase(Ease.OutCubic);
    }

    public void Hide()
    {
        float height = container.rect.height;
        if (height < 2f)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
            height = container.rect.height;
        }

        float yOffset = 200f;
        containerHiddenPos = new Vector2(0, -height + yOffset);

        container.DOAnchorPos(containerHiddenPos, animationTime)
            .SetEase(Ease.InCubic)
            .OnComplete(HideInstant);
    }

    public void HideInstant()
    {
        panelBackground.SetActive(false);
        container.gameObject.SetActive(false);
    }
}
