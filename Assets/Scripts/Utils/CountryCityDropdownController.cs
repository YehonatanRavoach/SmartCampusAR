using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Coordinates searchable dropdowns for country and city selection in forms.
/// Loads a mapping from country to cities from a local JSON file.
/// Updates city options based on selected country and exposes selection events.
/// </summary>
public class CountryCityDropdownController : MonoBehaviour
{
    [Header("Dropdowns")]
    public SearchDropdown countryDropdown;
    public SearchDropdown cityDropdown;

    [Header("Events")]
    public Action<string> OnCountrySelected;
    public Action<string> OnCitySelected;

    private Dictionary<string, List<string>> citiesByCountry;
    private string selectedCountry = "";
    private string selectedCity = "";

    private void Awake()
    {
        LoadCitiesFromJson();

        countryDropdown.OnOptionSelected = country =>
        {
            selectedCountry = country;
            OnCountrySelected?.Invoke(country);

            // Set cities
            if (citiesByCountry.ContainsKey(country))
            {
                var cities = citiesByCountry[country];
                cityDropdown.SetOptions(cities);
            }
            else
            {
                cityDropdown.SetOptions(new List<string>());
            }

            selectedCity = "";
            cityDropdown.SetInteractable(true);
        };

        cityDropdown.OnOptionSelected = city =>
        {
            selectedCity = city;
            OnCitySelected?.Invoke(city);
        };

        cityDropdown.SetInteractable(false);
    }

    private void LoadCitiesFromJson()
    {
        TextAsset json = Resources.Load<TextAsset>("cities_by_country");
        if (json == null)
        {
            Debug.LogError("cities_by_country.json not found in Resources/");
            return;
        }

        citiesByCountry = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json.text);
        countryDropdown.SetOptions(citiesByCountry.Keys.OrderBy(x => x).ToList());
    }

    public string GetSelectedCountry() => selectedCountry;
    public string GetSelectedCity() => selectedCity;
}

/*
    --- Key Concepts ---

    - Dynamic Dropdowns: Populates city dropdown based on selected country.
    - Data Source: Loads country/city mapping from a local JSON file (resources).
    - Events: Exposes OnCountrySelected and OnCitySelected for parent script logic.
    - Integration: Used in campus registration for country and city selection (with SearchDropdown).
    - User Experience: Prevents city selection before country is chosen and ensures options are relevant and searchable.
*/