using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * BuildingDataHolder.cs
 * -----------------------------------------------------------------------------
 * A tiny static container used to pass the selected **building document ID**
 * between scenes (e.g., from a list or map scene into the AR-navigation scene).
 *
 *  • Because it is purely static, its contents persist only while the game
 *    session is running; it resets when the app restarts or the domain reloads.
 *  • The default empty string (“”) indicates that no building is currently
 *    selected.
 */

public static class BuildingDataHolder
{
    public static string docId = "";
}
