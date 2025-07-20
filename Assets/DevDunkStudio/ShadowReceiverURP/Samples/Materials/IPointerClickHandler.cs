using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageClickHandler : MonoBehaviour, IPointerClickHandler
{
    public RectTransform parentForButtons; // לאן נצמיד את הכפתורים החדשים
    public GameObject buttonPrefab;        // Prefab של כפתור עגול

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1) חשב את המיקום המקומי (Local Position) בתמונה
        RectTransform rectTransform = GetComponent<RectTransform>();

        Vector2 localPoint;
        // ScreenPointToLocalPointInRectangle: ממיר נקודת מסך לנקודה בתוך ה-RectTransform
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint))
        {
            // localPoint מחזיק את המיקום היחסי בתוך ה-Image

            // 2) צור כפתור עגול חדש
            GameObject newButton = Instantiate(buttonPrefab, parentForButtons);

            // 3) קבע את ה-AnchoredPosition של הכפתור
            RectTransform newBtnRect = newButton.GetComponent<RectTransform>();
            newBtnRect.anchoredPosition = localPoint; 

            // במידה ורוצים את הכפתור בדיוק מעל ה-Image,
            // אפשר להציב אותו כילד של ה-Image עצמו
            // newButton.transform.SetParent(rectTransform, false);

            // אפשר גם להוסיף לוגיקה נוספת: גודל, צבע, אנימציה, וכד’
        }
    }
}
