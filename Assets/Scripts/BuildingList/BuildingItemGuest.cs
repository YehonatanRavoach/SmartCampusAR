using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
/*
 * BuildingItemGuest.cs
 * -----------------------------------------------------------------------------
 * Represents a single building entry in the guest view list.
 * Displays the building’s name, description, and thumbnail, and handles:
 * - Row click: navigates to AR navigation scene
 * - Info button click: shows detailed info panel with full data
 * - Asynchronous thumbnail loading and caching for reuse
 */

public class BuildingItemGuest : MonoBehaviour
{
    
    [Header("UI References")]
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingDescriptionText;
    public Image           buildingImage;
    public Button          infoButton;

    
    [Header("Info-Panel Prefab")]
    public GameObject infoPanelPrefab;

   
    public  string  BuildingName { get; private set; }
    private string  buildingId;
    private string  description;
    private string  coordinateString;
    private string  imageUrl;       
    private Sprite  loadedSprite;   

    /*─────────────────────── Init ───────────────────────*/
    public void Setup(string buildingId,
                      string name,
                      string desc,
                      string imageUrl,
                      string coords)
    {
        this.buildingId       = buildingId;
        this.BuildingName     = name;
        this.description      = desc;
        this.coordinateString = coords;
        this.imageUrl         = imageUrl;

        if (buildingNameText)        buildingNameText.text = name;
        if (buildingDescriptionText) buildingDescriptionText.text = desc;

        
        if (TryGetComponent<Button>(out var rowBtn))
        {
            rowBtn.onClick.RemoveAllListeners();
            rowBtn.onClick.AddListener(OnRowClicked);
        }

        
        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(OnInfoClicked);
        }

        
        if (!string.IsNullOrEmpty(imageUrl))
            StartCoroutine(LoadImage(imageUrl, buildingImage, true)); // true → לשמור לספרייט
    }// Initialize the item with building data

    /*─────────────────────── Callbacks ───────────────────────*/
    private void OnRowClicked()
    {
        BuildingDataHolder.docId = buildingId;
        SceneManager.LoadScene("ARnavigation");
    }

    private void OnInfoClicked()
    {
        if (infoPanelPrefab == null)
        {
            Debug.LogError("⚠️ infoPanelPrefab לא משויך ב-Inspector");
            return;
        }

        GameObject panelGO = Instantiate(infoPanelPrefab, transform.root);
        var ctrl = panelGO.GetComponent<BuildingInfoPanelController>();

        if (ctrl == null)
        {
            Debug.LogError("⚠️ Prefab חסר BuildingInfoPanelController");
            return;
        }

        
        ctrl.Show(BuildingName,
              description,
              coordinateString,
              loadedSprite,
              imageUrl);
    }// Handle row click to navigate to AR scene and info button click to show details

    /*─────────────────────── Helpers ───────────────────────*/
    
    private IEnumerator LoadImage(string url, Image targetImg, bool keepSprite)
    {
        using var uwr = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
        yield return uwr.SendWebRequest();

        if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(uwr);
            var sprite = Sprite.Create(tex,
                                       new Rect(0, 0, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f));

            targetImg.sprite         = sprite;
            targetImg.preserveAspect = false;

            if (keepSprite)
                loadedSprite = sprite; 
        }
        else
        {
            Debug.LogWarning("⚠️ Image load failed: " + uwr.error);
        }
    }
}
