

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
/*
 * MapPointNavigation.cs
 * -----------------------------------------------------------------------------
 * Represents an interactive pin on the campus map. Each pin displays a building
 * name and short description; clicking it stores the selected building ID and
 * navigates the user to the “ARnavigation” scene.
 */
 
public class MapPointNavigation : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI buildingNameText;
    public TextMeshProUGUI buildingDescriptionText;
    
 
    public string BuildingName { get; private set; }
    private string docId;
 
    public void Setup(string documentId, string name, string desc)
    {
        docId         = documentId;
        BuildingName  = name;
 
        buildingNameText.text        = name;
        buildingDescriptionText.text = desc;
 
        // wire up the click
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            // remove any old listeners to avoid duplicates
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnPointerClick);
        }
 
       
    }
 
    public void OnPointerClick()
    {
        // save into the static holder
        BuildingDataHolder.docId = docId;
        Debug.Log("Clicked building id: " + docId);
 
        SceneManager.LoadScene("ARnavigation");
    }
 
    
}

 