// CampusStartItem.cs
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
/*
 * CampusStartItem.cs
 * -----------------------------------------------------------------------------
 * UI component for a single “campus card” in the start screen.  
 * Handles thumbnail loading and scene‑navigation when tapped.
 *
 *  Public API
 *  ----------
 *      void Setup(string name, string docId, string logoURL)
 *          Populates the row and wires the button click.
 *
 *  Runtime flow
 *  ------------
 *  • `Setup()` assigns the campus name, stores the Firestore doc‑ID, and kicks
 *    off a coroutine to download the campus logo.  
 *  • On click, the doc‑ID and name are copied into <see cref="CampusDataHolder">
 *    and the scene “BuildingsList” is loaded.  
 */


public class CampusStartItem : MonoBehaviour
{
    public TextMeshProUGUI campusNameText;
    private string campusDocId;

    public Image campusLogogImage;

    public void Setup(string name, string docId, string imageUrl)
    {
        // wire up the click
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            // remove any old listeners to avoid duplicates
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnCampusClicked);
        }

        // set the text and image
    
        campusNameText.text = name;
        campusDocId = docId;

        if (!string.IsNullOrEmpty(imageUrl))
            StartCoroutine(LoadImage(imageUrl, campusLogogImage));
    }

    public void OnCampusClicked()
    {
        if (string.IsNullOrEmpty(campusDocId))
        {
            Debug.LogError("Campus ID is null or empty");
            return;
        }

        CampusDataHolder.selectedCampusId = campusDocId;
        CampusDataHolder.selectedCampusName = campusNameText.text;

        Debug.Log("Navigating to BuildingsList scene with ID: " + campusDocId);
        SceneManager.LoadScene("BuildingsList");
    }

    System.Collections.IEnumerator LoadImage(string url, Image image)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                // Get the downloaded texture
                Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)request.downloadHandler).texture;

                // Create a sprite from the texture
                image.sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f)
                );

                // Adjust the image RectTransform to fill the parent space (customize as needed)
                RectTransform rectTransform = image.GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;   // (0,0)
                rectTransform.anchorMax = Vector2.one;    // (1,1)
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                image.preserveAspect = false;
            }
            else
            {
                Debug.LogError("Failed to load image: " + request.error);
                Debug.LogError("Failed to load image: " + request.error);
            }
        }
    }
}
    