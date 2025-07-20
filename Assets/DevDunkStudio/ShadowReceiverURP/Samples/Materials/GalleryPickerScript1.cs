using UnityEngine;
using UnityEngine.UI;
using System; // לשימוש ב-DateTime

public class GalleryPickerScript1 : MonoBehaviour
{
    [Header("Optional Preview UI")]
    public Image previewImage; // אם תרצה להציג את התמונה שנבחרה

    // פונקציה שמחברים לכפתור OnClick
    public void PickAndSavePhoto()
    {
        #if UNITY_ANDROID || UNITY_IOS
        // פותח את הגלריה ומאפשר למשתמש לבחור תמונה
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                // path מחזיק את נתיב הקובץ שנבחר בגלריה
                Debug.Log("תמונה נבחרה מהגלריה: " + path);

                // 1) טוען את התמונה כ-Texture2D
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 1024);
                if (texture == null)
                {
                    Debug.LogError("טעינת התמונה נכשלה (יתכן פורמט לא נתמך או בעיה בקובץ).");
                    return;
                }

                // (אופציונלי) מציג את התמונה ב-UI Image
                if (previewImage != null)
                {
                    Sprite sprite = Sprite.Create(
                        texture, 
                        new Rect(0, 0, texture.width, texture.height),
                        Vector2.zero
                    );
                    previewImage.sprite = sprite;
                }

                // 2) שמירה לגלריה (שומר עותק חדש)
                string fileName = "SelectedPhoto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
                NativeGallery.SaveImageToGallery(
                    texture, // הטקסטורה
                    "MyApp", // שם תיקייה לגלריה
                    fileName,
                    (success, savePath) =>
                    {
                        Debug.Log("שמירת תמונה לגלריה - הצלחה? " + success + " | נתיב: " + savePath);
                    }
                );
            }
            else
            {
                Debug.Log("משתמש ביטל או לא נבחרה תמונה.");
            }
        }, "בחר תמונה מהגלריה");

        #else
        Debug.LogWarning("PickAndSavePhoto() נתמך רק באנדרואיד/iOS באמצעות NativeGallery.");
        #endif
    }
}
