using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * CampusDataHolder.cs
 * -----------------------------------------------------------------------------
 * Holds the user’s current campus selection so it can be accessed from any scene.
 * Store a value here when the user chooses a campus; read the same value later
 * (e.g., when loading buildings for that campus).
 */

public static class CampusDataHolder
{
    public static string selectedCampusId;
    public static string selectedCampusName;
}

