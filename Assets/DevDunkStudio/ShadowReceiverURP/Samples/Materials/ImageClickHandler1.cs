using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageClickHandler1 : MonoBehaviour, IPointerClickHandler
{
    public GameObject buttonPrefab;       // ה-Prefab של הכפתור העגול
    public RectTransform parentForButtons; // הורה לכפתור (Canvas / Panel)

    private GameObject singleButton;      // רפרנס לכפתור היחיד שאנחנו יוצרים

    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform imageRect = GetComponent<RectTransform>();

        Vector2 localPoint;
        bool isInside = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            imageRect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        if (isInside)
        {
            if (singleButton == null)
            {
                // במקרה שהכפתור עוד לא נוצר, ניצור אותו בפעם הראשונה
                singleButton = Instantiate(buttonPrefab);

                // להגדיר הורה
                if (parentForButtons != null)
                {
                    singleButton.transform.SetParent(parentForButtons, false);
                }
                else
                {
                    singleButton.transform.SetParent(imageRect, false);
                }
            }

            // בין אם הכפתור נוצר עכשיו או שכבר קיים – נעדכן מיקום
            RectTransform buttonRect = singleButton.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = localPoint;
        }
    }
}
