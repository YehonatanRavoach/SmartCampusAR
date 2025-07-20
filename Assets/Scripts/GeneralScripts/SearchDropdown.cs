using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
/******************************************************************************
 * SearchDropdown.cs
 * -----------------------------------------------------------------------------
 * Re‑usable “search‑type‑ahead” component for TextMeshPro InputFields.
 *
 *  Features
 *  --------
 *  • Displays a scrollable dropdown under the input field as soon as the user
 *    focuses the field (**ExpandDropdown**).  
 *  • Filters `allOptions` in real‑time while the user types
 *    (**ShowFilteredOptions**).  
 *  • Highlights matches that *start with* the typed substring first, then the
 *    rest, both alphabetically.  
 *  • Invokes <see cref="OnOptionSelected"/> callback with the chosen string
 *    and updates the input text accordingly.  
 *  • Handles moving the dropdown onto a separate “popup layer” canvas so that
 *    it renders above other UI (via optional **CanvasMover** helper).  
 *  • Public **SetInteractable** lets a parent form enable/disable the whole
 *    widget.  
 *
 *  Inspector wiring
 *  ----------------
 *      searchInput       – TMP_InputField the user types into  
 *      optionPrefab      – Prefab with Button + TMP_Text for each row  
 *      optionsContainer  – Vertical Layout Group (content of the ScrollRect)  
 *      scrollRect        – ScrollRect that holds the list (initially inactive)  
 *      canvasMover       – Optional helper to shift the parent canvas upward
 *                          when the software keyboard appears  
 *      popupLayer        – Transform under which the dropdown is temporarily
 *                          re‑parented (e.g. a dedicated overlay canvas)  
 *      fieldReference    – RectTransform of the input field (for positioning)  
 *
 *****************************************************************************/
public class SearchDropdown : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField searchInput;
    public GameObject optionPrefab;
    public Transform optionsContainer;
    public ScrollRect scrollRect;

    public CanvasMover canvasMover;


    [Header("Logic")]
    public Action<string> OnOptionSelected; // Callback when an option is selected

    public Transform popupLayer;
    public RectTransform fieldReference; // RectTransform of the input field to position the dropdown below it
    private Transform originalParent;
    private Vector3 originalPosition;

    private List<string> allOptions = new(); // Full list of options to filter from
    private readonly List<GameObject> activeOptions = new(); // Currently displayed options
     

    private void Awake()
    {
        scrollRect.gameObject.SetActive(false);
        searchInput.onSelect.AddListener(_ => ExpandDropdown());
        searchInput.onValueChanged.AddListener(ShowFilteredOptions);
    }





    public void SetOptions(List<string> options) // Set the full list of options to filter from
    {
        allOptions = options.OrderBy(x => x).ToList();
        searchInput.text = ""; // clear previous text
        scrollRect.gameObject.SetActive(false);
        ClearOptions();

        if (!string.IsNullOrWhiteSpace(searchInput.text))
        {
            ShowFilteredOptions(searchInput.text);
        }
    }

    public void ExpandDropdown() // Show the dropdown when the input field is focused
    {
        if (allOptions == null || allOptions.Count == 0)
            return;

        ShowFilteredOptions("");

        originalParent = scrollRect.transform.parent;
        originalPosition = scrollRect.transform.position;

        RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();

        Vector3 fieldBottomWorld = fieldReference.TransformPoint(
             new Vector3(0, -fieldReference.rect.height / 2f, 0)
         );

        scrollRect.transform.SetParent(popupLayer, worldPositionStays: false);

        float extraOffset = -40f;
        float scrollHeight = scrollRectTransform.rect.height;

        scrollRectTransform.position = fieldBottomWorld + new Vector3(0, -scrollHeight / 2f + extraOffset, 0);

        if (canvasMover != null)
        {
            canvasMover.MoveCanvasUp();
            

        }


        scrollRect.gameObject.SetActive(true);
        
    }


    private void ShowFilteredOptions(string input) // Filter the options based on user input
    {
        ClearOptions();

        string inputClean = input.Trim().ToLower();

        var matches = allOptions
            .Where(o => o.ToLower().Contains(inputClean))
            .OrderBy(o => !o.ToLower().StartsWith(inputClean))
            .ThenBy(o => o)
            .ToList();

        scrollRect.gameObject.SetActive(matches.Count > 0);

        foreach (var match in matches)
        {
            GameObject item = Instantiate(optionPrefab, optionsContainer);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();
            label.text = match;

            item.GetComponent<Button>().onClick.AddListener(() =>
            {
                searchInput.text = match;

                scrollRect.gameObject.SetActive(false);
                if (canvasMover != null)
                {

                    canvasMover.ResetCanvasPosition();
                    
                     
                }

                scrollRect.transform.SetParent(originalParent, worldPositionStays: true);

                OnOptionSelected?.Invoke(match);
            });

            activeOptions.Add(item);
        }
    }


    private void ClearOptions() // Clear the currently displayed options
    {
        foreach (GameObject obj in activeOptions)
            Destroy(obj);
        activeOptions.Clear();
    }

    public void SetInteractable(bool value) // Enable or disable the search input and dropdown
    {
        searchInput.interactable = value;
        searchInput.text = value ? "" : ""; // Optional: clear field if disabling
        scrollRect.gameObject.SetActive(false);
    }

}
