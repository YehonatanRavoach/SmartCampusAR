using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
/*
 * BuildingInfoPanelController.cs
 * -----------------------------------------------------------------------------
 * Controls the UI panel displaying detailed information about a building,
 * including title, description, coordinates, and an image. Supports lazy-loading
 * remote images with a loading spinner, and handles panel closing.
 */

public class BuildingInfoPanelController : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI titleText; 
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI coordinatesText;
    public Image           buildingImage;
    public Button          closeButton;

    
    private string   pendingUrl;
    public GameObject loadingSpinner;

    /* --------------------- Awake --------------------- */
    private void Awake()
    {
        
        if (closeButton) closeButton.onClick.AddListener(OnClose); // Add close button listener
        
        gameObject.SetActive(false);  // Initially hide the panel      
    }

    /* --------------------- Show ---------------------- */
    public void Show(string title,
                     string description,
                     string coordText,
                     Sprite sprite,
                     string fallbackUrl)
    {
        
        titleText.text       = title;
        descriptionText.text = string.IsNullOrWhiteSpace(description)
                               ? "(No description)" : description;
        coordinatesText.text = coordText;

        
        if (sprite != null)
        {
            buildingImage.sprite  = sprite;
            buildingImage.enabled = true;
        }
        else
        {
            buildingImage.enabled = false;
            pendingUrl            = fallbackUrl;   
        }

        gameObject.SetActive(true);                
    }// Show the panel with provided data

    /* -------------------- OnEnable ------------------- */
    private void OnEnable()
    {
        if (!string.IsNullOrEmpty(pendingUrl))
        {
            
            StartCoroutine(LoadAndSet(pendingUrl));
            pendingUrl = null; 
                    
        }
    }// Called when the panel is enabled

    /* ---------------- Load & set --------------------- */
    private IEnumerator LoadAndSet(string url)
    {
        loadingSpinner.SetActive(true);
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(uwr);
                Sprite spr = Sprite.Create(tex,
                                  new Rect(0, 0, tex.width, tex.height),
                                  new Vector2(0.5f, 0.5f));

                buildingImage.sprite = spr;
                buildingImage.enabled = true;
            }
            else
            {
                Debug.LogWarning("Image download failed: " + uwr.error);
            }
        }
        loadingSpinner.SetActive(false);
    }

    private void OnClose() => Destroy(gameObject);
}
