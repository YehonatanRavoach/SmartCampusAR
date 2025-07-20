using TMPro;
using UnityEngine;
/*
 * InfoDisplay.cs
 * ------------------------------------------------------------------------
 * A simple MonoBehaviour that displays formatted information in a TextMeshPro
 * component. It supports different types of information (e.g., temperature,
 * humidity, etc.) and applies color coding for better visibility.
 *
 *  • Call <see cref="SetInfo(string, float)"/> to update the displayed info.
 */
public class InfoDisplay : MonoBehaviour
{
    public TextMeshPro infoText;

public void SetInfo(string type, float value)
{
    type = type.ToLower(); // Normalize

    switch (type)
    {
        case "temperature":
            infoText.text = $"<color=#ff5733>🌡️ Temperature:</color> {value}°C";
            break;

        case "humidity":
            infoText.text = $"<color=#00aaff>💧 Humidity:</color> {value}%";
            break;

        case "precipitation":
            infoText.text = $"<color=#4a90e2>🌧️ Precipitation:</color> {value} mm";
            break;

        case "wind":
            infoText.text = $"<color=#00cc66>💨 Wind:</color> {value} km/h";
            break;

        case "uv":
            infoText.text = $"<color=#ffaa00>☀️ UV Index:</color> {value}";
            break;

        default:
            infoText.text = $"<color=#cccccc>{type.ToUpper()}:</color> {value}";
            break;
    }
}
}
